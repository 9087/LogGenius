using System.Diagnostics;

namespace LogGenius.Core
{
    public class Entry
    {
        public string Text { get; set; } = string.Empty;

        public uint Line { get; set; }

        public Entry()
        {
        }

        public Entry(string InText)
        {
            Text = InText;
        }

        public Entry(string InText, uint InLine)
        {
            Text = InText;
            Line = InLine;
        }

        private Dictionary<Type, object> MetaDatas = new();

        public T AddMetaData<T>() where T : class, new()
        {
            Debug.Assert(!MetaDatas.ContainsKey(typeof(T)));
            var MetaData = new T();
            MetaDatas.Add(typeof(T), MetaData);
            return MetaData;
        }

        public T? GetMetaData<T>() where T : class, new()
        {
            if (MetaDatas.TryGetValue(typeof(T), out var MetaData))
            {
                return (T)MetaData;
            }
            return null;
        }

        public T GetOrAddMetaData<T>() where T : class, new()
        {
            var MetaData = GetMetaData<T>();
            if (MetaData != null)
            {
                return MetaData;
            }
            return AddMetaData<T>();
        }
    }
}
