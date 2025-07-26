using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LogGenius.Core
{
    public partial class CoreModule : Module<CoreModule>
    {
        public CoreModule(Session Session) : base(Session)
        {
        }

        [ObservableProperty]
        [Setting]
        protected ObservableCollection<string> _RecentFiles = new();

        public void RaiseOnFileOpened(string FilePath)
        {
            RecentFiles.Remove(FilePath);
            RecentFiles.Insert(0, FilePath);
            while (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
            OnPropertyChanged(nameof(RecentFiles));
        }
    }
}
