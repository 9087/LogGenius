using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LogGenius.Core
{
    public abstract class ModuleBase : ObservableObject
    {
        private Manager? Manager;

        internal void SetManager(Manager Manager)
        {
            this.Manager = Manager;
        }

        protected Session Session;

        private List<PropertyInfo>? SettingPropertyInfos = new();

        protected ModuleBase(Session Session)
        {
            this.Session = Session;
            InitializeSettingPropertyInfos();
        }

        private void InitializeSettingPropertyInfos()
        {
            var ModuleType = GetType();
            foreach (var PropertyInfo in ModuleType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var SettingAttribute = PropertyInfo.GetCustomAttribute<SettingAttribute>();
                if (SettingAttribute == null)
                {
                    continue;
                }
                SettingPropertyInfos?.Add(PropertyInfo);
            }
            foreach (var FieldInfo in ModuleType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var SettingAttribute = FieldInfo.GetCustomAttribute<SettingAttribute>();
                if (SettingAttribute == null)
                {
                    continue;
                }
                var ObservablePropertyAttribute = FieldInfo.GetCustomAttribute<ObservablePropertyAttribute>();
                if (ObservablePropertyAttribute == null)
                {
                    continue;
                }
                var Prefixes = new List<string> { "m_", "_" };
                var FieldName = FieldInfo.Name;
                foreach (var Prefix in Prefixes)
                {
                    if (FieldName.StartsWith(Prefix))
                    {
                        FieldName = FieldName.Substring(Prefix.Length);
                        break;
                    }
                }
                if (string.IsNullOrEmpty(FieldName))
                {
                    continue;
                }
                var PropertyName = char.ToUpper(FieldName[0]) + FieldName.Substring(1);
                var PropertyInfo = ModuleType.GetProperty(PropertyName);
                if (PropertyInfo != null)
                {
                    SettingPropertyInfos?.Add(PropertyInfo);
                }
            }
        }

        internal JsonObject Serialize()
        {
            var JsonSerializerOptions = new JsonSerializerOptions();
            JsonSerializerOptions.IgnoreReadOnlyFields = true;
            JsonSerializerOptions.IgnoreReadOnlyProperties = true;
            var JsonObject = new JsonObject();
            foreach (var SettingPropertyInfo in SettingPropertyInfos!)
            {
                JsonObject.Add(
                    SettingPropertyInfo.Name,
                    JsonSerializer.SerializeToNode(SettingPropertyInfo.GetValue(this), JsonSerializerOptions)
                );
            }
            return JsonObject;
        }

        internal void Deserialize(JsonObject JsonObject)
        {
            var JsonSerializerOptions = new JsonSerializerOptions();
            JsonSerializerOptions.IgnoreReadOnlyFields = true;
            JsonSerializerOptions.IgnoreReadOnlyProperties = true;
            foreach (var SettingPropertyInfo in SettingPropertyInfos!)
            {
                if (!JsonObject.TryGetPropertyValue(SettingPropertyInfo.Name, out var JsonNode))
                {
                    continue;
                }
                SettingPropertyInfo.SetValue(
                    this,
                    JsonSerializer.Deserialize(JsonNode, SettingPropertyInfo.PropertyType, JsonSerializerOptions)
                );
            }
        }
    }

    public abstract class Module<T> : ModuleBase where T : ModuleBase
    {
        public static T Instance => Manager.Instance.GetModuleChecked<T>();

        protected Module(Session Session) : base(Session)
        {
        }
    }
}