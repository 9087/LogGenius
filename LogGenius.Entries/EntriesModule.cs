using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogGenius.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Data;
using System.Windows.Threading;

namespace LogGenius.Modules.Entries
{
    public partial class EntriesModule : LogGenius.Core.Module<EntriesModule>
    {
        public CollectionViewSource FullEntriesViewSource { get; } = new();

        public CollectionViewSource FilteredEntriesViewSource { get; } = new();

        [ObservableProperty]
        [Setting]
        protected string _FilterPattern = string.Empty;

        [ObservableProperty]
        [Setting]
        private bool _IsCaseSensitive = false;

        [ObservableProperty]
        [Setting]
        private bool _IsRegex = false;

        [ObservableProperty]
        [Setting]
        private ObservableCollection<EntryStyle> _EntryStyles = new();

        public EntriesModule(Session Session) : base(Session)
        {
            FullEntriesViewSource.Source = Session.Entries;
            FilteredEntriesViewSource.Source = Session.Entries;
            FilteredEntriesViewSource.Filter += OnFiltering;
            Session.EntriesCleared += OnEntriesCleared;
            Session.EntryCreated += OnEntryCreated;
            EntryMatchingState = new(FilterPattern, IsCaseSensitive, IsRegex);
        }

        ~EntriesModule()
        {
            Session.EntriesCleared -= OnEntriesCleared;
            Session.EntryCreated -= OnEntryCreated;
        }

        private void OnEntriesCleared()
        {
            CancelExcluding();
        }

        [RelayCommand]
        private void FilterPatternChanged()
        {
            FilteredEntriesViewSource.View.Refresh();
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs EventArgs)
        {
            base.OnPropertyChanged(EventArgs);
            switch (EventArgs.PropertyName)
            {
                case nameof(FilterPattern):
                case nameof(IsCaseSensitive):
                case nameof(IsRegex):
                    lock (EntryMatchingState)
                    {
                        EntryMatchingState = new(FilterPattern, IsCaseSensitive, IsRegex);
                    }
                    break;
            }
            switch (EventArgs.PropertyName)
            {
                case nameof(IsCaseSensitive):
                case nameof(IsRegex):
                    FilteredEntriesViewSource.View.Refresh();
                    break;
            }
        }

        private void OnFiltering(object sender, FilterEventArgs e)
        {
            if (e.Item is Entry Entry)
            {
                if (ExcludingIndex != null)
                {
                    if (Entry.Line <= ExcludingIndex)
                    {
                        e.Accepted = false;
                        return;
                    }
                }
                e.Accepted = Entry.Test(EntryMatchingState);
            }
            else
            {
                e.Accepted = false;
            }
        }

        [ObservableProperty]
        public EntryStyle? _CurrentEntryStyle = null;

        [ObservableProperty]
        public EntryStyle _EditableEntryStyle = new();

        public bool IsEditableEntryStyleAvailable => !string.IsNullOrEmpty(EditableEntryStyle.FilterPattern);

        public bool IsEditableEntryStyleAbleToBeAdded => IsEditableEntryStyleAvailable;

        public bool IsEditableEntryStyleAbleToBeApplied => CurrentEntryStyle != null && IsEditableEntryStyleAvailable;

        public bool IsCurrentEntryStyleAbleToBeDeleted => CurrentEntryStyle != null;

        public bool IsCurrentEntryStyleAbleToBeMovedUp => CurrentEntryStyle != null && EntryStyles.IndexOf(CurrentEntryStyle) > 0;

        public bool IsCurrentEntryStyleAbleToBeMovedDown => CurrentEntryStyle != null && EntryStyles.IndexOf(CurrentEntryStyle) >= 0 && EntryStyles.IndexOf(CurrentEntryStyle) < EntryStyles.Count - 1;

        [RelayCommand]
        void EntryStylesSelectionChanged()
        {
            AddEntryStyleCommand.NotifyCanExecuteChanged();
            ApplyEntryStyleCommand.NotifyCanExecuteChanged();
            DeleteCurrentEntryStyleCommand.NotifyCanExecuteChanged();
            MoveUpCurrentEntryStyleCommand.NotifyCanExecuteChanged();
            MoveDownCurrentEntryStyleCommand.NotifyCanExecuteChanged();
            if (CurrentEntryStyle != null)
            {
                EditableEntryStyle = SpawnEntryStyle(CurrentEntryStyle);
            }
        }

        [RelayCommand]
        void FilterPatternTextChanged()
        {
            AddEntryStyleCommand.NotifyCanExecuteChanged();
            ApplyEntryStyleCommand.NotifyCanExecuteChanged();
            DeleteCurrentEntryStyleCommand.NotifyCanExecuteChanged();
            MoveUpCurrentEntryStyleCommand.NotifyCanExecuteChanged();
            MoveDownCurrentEntryStyleCommand.NotifyCanExecuteChanged();
        }

        static public EntryStyle SpawnEntryStyle(EntryStyle Source)
        {
            var JsonSerializerOptions = new JsonSerializerOptions();
            JsonSerializerOptions.IgnoreReadOnlyFields = true;
            JsonSerializerOptions.IgnoreReadOnlyProperties = true;
            var JsonNode = JsonSerializer.SerializeToNode(Source, JsonSerializerOptions);
            return (EntryStyle)JsonSerializer.Deserialize(JsonNode, typeof(EntryStyle), JsonSerializerOptions)!;
        }

        [RelayCommand(CanExecute = nameof(IsEditableEntryStyleAbleToBeAdded))]
        public void AddEntryStyle()
        {
            var NewEntryStyle = SpawnEntryStyle(EditableEntryStyle);
            EntryStyles.Add(NewEntryStyle);
            CurrentEntryStyle = NewEntryStyle;
            EntryStylesSelectionChanged();
        }

        [RelayCommand(CanExecute = nameof(IsEditableEntryStyleAbleToBeApplied))]
        public void ApplyEntryStyle()
        {
            var Index = EntryStyles.IndexOf(CurrentEntryStyle!);
            var NewEntryStyle = SpawnEntryStyle(EditableEntryStyle);
            EntryStyles[Index] = NewEntryStyle;
            CurrentEntryStyle = NewEntryStyle;
            EntryStylesSelectionChanged();
        }

        [RelayCommand(CanExecute = nameof(IsCurrentEntryStyleAbleToBeDeleted))]
        public void DeleteCurrentEntryStyle()
        {
            var Index = EntryStyles.IndexOf(CurrentEntryStyle!);
            EntryStyles.Remove(CurrentEntryStyle!);
            if (Index <= EntryStyles.Count - 1)
            {
                CurrentEntryStyle = EntryStyles[Index];
            }
            else if (EntryStyles.Count > 0)
            {
                CurrentEntryStyle = EntryStyles[EntryStyles.Count - 1];
            }
            else
            {
                CurrentEntryStyle = null;
            }
            OnPropertyChanged(nameof(CurrentEntryStyle));
            EntryStylesSelectionChanged();
        }

        [RelayCommand(CanExecute = nameof(IsCurrentEntryStyleAbleToBeMovedUp))]
        public void MoveUpCurrentEntryStyle()
        {
            var Index = EntryStyles.IndexOf(CurrentEntryStyle!);
            EntryStyles.Move(Index, Index - 1);
            EntryStylesSelectionChanged();
        }

        [RelayCommand(CanExecute = nameof(IsCurrentEntryStyleAbleToBeMovedDown))]
        public void MoveDownCurrentEntryStyle()
        {
            var Index = EntryStyles.IndexOf(CurrentEntryStyle!);
            EntryStyles.Move(Index, Index + 1);
            EntryStylesSelectionChanged();
        }

        [RelayCommand]
        void ConfirmHighlightingSettingDialog()
        {
            CurrentEntryStyle = null;
            EditableEntryStyle = new();
            Manager.Instance.SaveSettings();
            FullEntriesViewSource.View.Refresh();
            FilteredEntriesViewSource.View.Refresh();
        }

        [ObservableProperty]
        [Setting]
        private ObservableCollection<string> _FilterPatternSuggestions = new();

        public void UpdateFilterPatternSuggestion(string FilterPattern)
        {
            var Temporay = FilterPatternSuggestions.ToList();
            Temporay.Remove(FilterPattern);
            Temporay.Insert(0, FilterPattern);
            while (Temporay.Count > 1)
            {
                Temporay.RemoveAt(Temporay.Count - 1);
            }
            FilterPatternSuggestions = new(Temporay);
        }

        [RelayCommand]
        public void DeleteFilterPatternSuggestion(string FilterPattern)
        {
            var Temporay = FilterPatternSuggestions.ToList();
            Temporay.Remove(FilterPattern);
            FilterPatternSuggestions = new(Temporay);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsExcluding))]
        private int? _ExcludingIndex = null;

        public bool IsExcluding => ExcludingIndex != null;

        [RelayCommand]
        public void ExcludeExisting()
        {
            if (ExcludingIndex != null)
            {
                return;
            }
            ReexcludeExisting();
        }

        [RelayCommand]
        public void ReexcludeExisting()
        {
            ExcludingIndex = Session.Entries.Count - 1;
            FilteredEntriesViewSource.View.Refresh();
        }

        [RelayCommand]
        public void ExcludeFromEntry(Entry Entry)
        {
            ExcludingIndex = (int)Entry.Line;
            FilteredEntriesViewSource.View.Refresh();
        }

        [RelayCommand]
        public void ExcludeBeforeEntry(Entry Entry)
        {
            if (Entry.Line == 0)
            {
                CancelExcluding();
                return;
            }
            ExcludingIndex = (int)Entry.Line - 1;
            FilteredEntriesViewSource.View.Refresh();
        }

        [RelayCommand]
        public void CancelExcluding()
        {
            ExcludingIndex = null;
            FilteredEntriesViewSource.View.Refresh();
        }

        private EntryMatchingState EntryMatchingState;

        private void OnEntryCreated(Entry Entry)
        {
            lock (EntryMatchingState)
            {
                Entry.Test(EntryMatchingState);
            }
        }
    }
}
