using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace LogGenius.Core
{
    public partial class Manager : ObservableObject
    {
        private static Manager? _Instance = null;

        public static Manager Instance => Get();

        protected static Manager Get() => _Instance ??= new Manager();

        [ObservableProperty]
        protected List<ModuleBase> _Modules = new();

        [ObservableProperty]
        protected List<WindowItem> _WindowItems = new();

        [ObservableProperty]
        public Session _Session = new Session();

#if DEBUG
        public bool IsDebugMode => true;
#else
        public bool IsDebugMode => false;
#endif

        protected Manager()
        {
        }

        public bool ContainsModule<T>() where T : ModuleBase
        {
            return ContainsModule(typeof(T));
        }

        public bool ContainsModule(Type ModuleType)
        {
            Debug.Assert(ModuleType.IsSubclassOf(typeof(ModuleBase)));
            return Modules.Any((X) => ModuleType.IsInstanceOfType(X));
        }

        public T GetModuleChecked<T>() where T : ModuleBase
        {
            return (T)GetModuleChecked(typeof(T));
        }

        public ModuleBase GetModuleChecked(Type ModuleType)
        {
            return Modules.Find((X) => ModuleType.IsInstanceOfType(X))!;
        }

        public void RegisterModule<T>() where T : ModuleBase
        {
            RegisterModule(typeof(T));
        }

        public void RegisterModule(Type ModuleType)
        {
            Debug.Assert(ModuleType.IsSubclassOf(typeof(ModuleBase)));
            if (ContainsModule(ModuleType))
            {
                return;
            }
            var ModuleBase = (Activator.CreateInstance(ModuleType, Session) as ModuleBase)!;
            ModuleBase.SetManager(this);
            Modules.Add(ModuleBase);
        }

        public bool ContainsWindow<T>() where T : Window
        {
            return ContainsWindow(typeof(T));
        }

        public bool ContainsWindow(Type WindowType)
        {
            Debug.Assert(WindowType.IsSubclassOf(typeof(Window)));
            return WindowItems.Any((X) => X.WindowType == WindowType);
        }

        public void RegisterWindow<T>() where T : Window
        {
            RegisterWindow(typeof(T));
        }

        public void RegisterWindow(Type WindowType)
        {
            Debug.Assert(WindowType.IsSubclassOf(typeof(Window)));
            if (ContainsWindow(WindowType))
            {
                return;
            }
            var WindowItem = new WindowItem(WindowType);
            WindowItem.SetManager(this);
            WindowItems.Add(WindowItem);
        }

        public WindowItem? GetWindowItem<T>()
        {
            return GetWindowItem(typeof(T));
        }

        public WindowItem? GetWindowItem(Type WindowType)
        {
            return WindowItems.Find((X) => X.WindowType == WindowType);
        }

        public bool IsWindowOpened<T>() where T : Window
        {
            return IsWindowOpened(typeof(T));
        }

        public bool IsWindowOpened(Type WindowType)
        {
            Debug.Assert(WindowType.IsSubclassOf(typeof(Window)));
            return GetWindowItem(WindowType)?.IsOpened ?? false;
        }

        public void OpenWindow<T>() where T : Window, new()
        {
            OpenWindow(typeof(T));
        }

        public void OpenWindow(Type WindowType)
        {
            if (!ContainsWindow(WindowType))
            {
                RegisterWindow(WindowType);
            }
            if (IsWindowOpened(WindowType))
            {
                return;
            }
            var WindowItem = GetWindowItem(WindowType)!;
            WindowItem.OpenWindow();
        }

        public void CloseWindow<T>() where T : ModuleBase, new()
        {
            CloseWindow(typeof(T));
        }

        public void CloseWindow(Type WindowType)
        {
            if (!ContainsModule(WindowType))
            {
                return;
            }
            if (!IsWindowOpened(WindowType))
            {
                return;
            }
            GetWindowItem(WindowType)!.CloseWindow();
        }

        static private string LocalApplicationDataDirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogGenius");

        static private string SettingsFilePath => Path.Combine(LocalApplicationDataDirectoryPath, "Settings.json");

        private void EnsureLocalApplicationDataDirectory()
        {
            if (!Directory.Exists(LocalApplicationDataDirectoryPath))
            {
                Directory.CreateDirectory(LocalApplicationDataDirectoryPath);
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                return;
            }
            using (var Stream = new System.IO.FileStream(SettingsFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            {
                if (Stream != null)
                {
                    JsonNode? Node = null;
                    try
                    {
                        Node = JsonNode.Parse(Stream);
                    }
                    catch
                    {
                    }
                    
                    if (Node != null)
                    {
                        var Root = Node!.AsObject();
                        foreach (var (Name, ModuleScope) in Root)
                        {
                            var Found = Modules.Find(X => X.GetType().FullName! == Name);
                            if (Found == null)
                            {
                                continue;
                            }
                            var Module = Found!;
                            Module.Deserialize(ModuleScope!.AsObject());
                        }
                    }
                }
            }
        }

        [RelayCommand]
        public void SaveSettings()
        {
            EnsureLocalApplicationDataDirectory();
            var RootElement = new JsonObject();
            foreach (var Module in Modules)
            {
                RootElement.Add(Module.GetType().FullName!, Module.Serialize());
            }
            if (!Directory.Exists(LocalApplicationDataDirectoryPath))
            {
                Directory.CreateDirectory(LocalApplicationDataDirectoryPath);
            }
            using (var Stream = new System.IO.FileStream(SettingsFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (var Writer = new Utf8JsonWriter(Stream, new JsonWriterOptions() { Indented = true }))
            {
                RootElement.WriteTo(Writer);
            }
        }

        public void RegisterFromAssemblies(Uri SearchPath)
        {
            foreach (var AssemblyPath in Directory.GetFiles(SearchPath.AbsolutePath, "*.dll", SearchOption.AllDirectories))
            {
                var Assembly = System.Reflection.Assembly.LoadFrom(AssemblyPath);
                if (Assembly == null)
                {
                    continue;
                }
                foreach (var Type in Assembly.GetTypes())
                {
                    if (Type.IsAbstract)
                    {
                        continue;
                    }
                    if (Type.IsSubclassOf(typeof(ModuleBase)))
                    {
                        RegisterModule(Type);
                    }
                    if (Type.IsSubclassOf(typeof(Window)))
                    {
                        RegisterWindow(Type);
                    }
                }
            }
        }

        public void StartUp()
        {
            LoadSettings();
            SaveSettings();
            WindowItems.Sort(
                (Left, Right)
                =>
                Comparer<int>.Default.Compare(
                    Left.MenuOrder,
                    Right.MenuOrder
                )
            );
            foreach (var WindowItem in WindowItems)
            {
                if (WindowItem.ShouldBeOpenedOnStartup)
                {
                    WindowItem.OpenWindow();
                }
            }
        }

        [RelayCommand]
        public void Shutdown()
        {
            Application.Current.Shutdown();
        }
    }
}
