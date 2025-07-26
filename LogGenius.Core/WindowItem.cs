using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LogGenius.Core
{
    public partial class WindowItem : ObservableObject
    {
        private Manager? Manager;

        internal void SetManager(Manager Manager)
        {
            this.Manager = Manager;
        }

        public Type WindowType { get; }

        public string MenuTitle => WindowInfoAttribute.GetMenuTitle(WindowType);

        public int MenuOrder => WindowInfoAttribute.GetMenuOrder(WindowType);

        public bool ShouldBeOpenedOnStartup => WindowInfoAttribute.GetShouldBeOpenedOnStartup(WindowType);

        public bool IsOpened => Window != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOpened))]
        private Window? _Window;

        public WindowItem(Type WindowType)
        {
            this.WindowType = WindowType;
        }

        [RelayCommand]
        public void OpenWindow()
        {
            this.Window ??= (Activator.CreateInstance(WindowType) as Window)!;
            this.Window.Closed += OnWindowClosed;
            this.Window.Show();
            OnPropertyChanged(nameof(IsOpened));
        }

        private void OnWindowClosed(object? Sender, EventArgs EventArgs)
        {
            this.Window = null;
        }

        [RelayCommand]
        public void CloseWindow()
        {
            this.Window?.Close();
        }
    }
}
