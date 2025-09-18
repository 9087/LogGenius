using LogGenius.Core;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace LogGenius.Modules.Entries
{
    public class GetTitleTextFromExcludingIndex : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            return Value is int Integer ? $"Exclude Until {Integer}" : "Exclude Existing";
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GetToggleStateFromExcludingIndex : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            return Value is int Integer ? Integer >= 0 : false;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }

    [WindowInfo("Entries", -1, true)]
    public partial class EntriesWindow : LogGenius.Core.Window
    {
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

        private void OnFilterPatternAutoSuggestBoxTextChanged(ModernWpf.Controls.AutoSuggestBox Sender, ModernWpf.Controls.AutoSuggestBoxTextChangedEventArgs EventArgs)
        {
        }

        private void OnFilterPatternAutoSuggestBoxSuggestionChosen(ModernWpf.Controls.AutoSuggestBox Sender, ModernWpf.Controls.AutoSuggestBoxSuggestionChosenEventArgs EventArgs)
        {
        }

        private void OnFilterPatternAutoSuggestBoxQuerySubmitted(ModernWpf.Controls.AutoSuggestBox Sender, ModernWpf.Controls.AutoSuggestBoxQuerySubmittedEventArgs EventArgs)
        {            
            PART_FilterPatternAutoSuggestBox.IsSuggestionListOpen = false;
            EntriesModule.Instance.UpdateFilterPatternSuggestion(PART_FilterPatternAutoSuggestBox.Text);
        }
    }
}