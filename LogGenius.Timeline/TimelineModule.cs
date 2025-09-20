using CommunityToolkit.Mvvm.ComponentModel;
using LogGenius.Core;

namespace LogGenius.Modules.Timeline
{
    public partial class TimelineModule : LogGenius.Core.Module<TimelineModule>
    {
        [ObservableProperty]
        private Timeline _Timeline = new();

        public TimelineModule(Session Session) : base(Session)
        {
            Session.EntriesAdded += OnEntriesAdded;
            Session.EntriesCleared += OnEntriesCleared;
        }

        private void OnEntriesAdded(List<Entry> Entries)
        {
            Dictionary<string, PropertyIdentity> NewIdentities = new();
            foreach (var Entry in Entries)
            {
                NewIdentities.Clear();
                var HeaderInfo = Entry.GetHeaderInfo();
                if (HeaderInfo == null)
                {
                    return;
                }
                Timeline.UpdateTime(HeaderInfo.DateTime);
                var Records = Entry.GetRecords(this.Timeline, NewIdentities);
                if (Records == null || Records.Count == 0)
                {
                    continue;
                }
                foreach (var Record in Records)
                {
                    Timeline.AddRecord(HeaderInfo.DateTime, Record);
                }
            }
        }

        private void OnEntriesCleared()
        {
            Timeline = new();
        }
    }
}