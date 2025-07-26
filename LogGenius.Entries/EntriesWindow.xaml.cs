using LogGenius.Core;
using System.Windows.Controls;

namespace LogGenius.Modules.Entries
{
    [WindowInfo("Entries", -1, true)]
    public partial class EntriesWindow : LogGenius.Core.Window
    {
        public Session Project => (Session)this.DataContext;
        
        public EntriesWindow()
        {
            InitializeComponent();

            var OptionsMenuItem = new MenuItem() { Header = "Options", };
            {
                var HighlightingMenuItem = new MenuItem();
                HighlightingMenuItem.Header = "Highlighting...";
                HighlightingMenuItem.Click += OnHighlightingMenuItemClicked;
                OptionsMenuItem.Items.Add(HighlightingMenuItem);
            }
            PART_Frame.AddMenuItem(OptionsMenuItem);
        }

        private async void OnHighlightingMenuItemClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            var HighlightingSettingDialog = new HighlightingSettingDialog();
            HighlightingSettingDialog.DataContext = EntriesModule.Instance;
            await HighlightingSettingDialog.ShowAsync();
        }

        private void OnFilteredEntryGridEntryDoubleClicked(object Sender, EntryDoubleClickedEventArgs EventArgs)
        {
            PART_FullEntryGrid.SetSelectedItem(EventArgs.Item);
            PART_FullEntryGrid.ScrollIntoView(EventArgs.Item);
        }
    }
}