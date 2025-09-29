using CommunityToolkit.Mvvm.ComponentModel;
using LogGenius.Core;

namespace LogGenius.Modules.Timeline
{
    public class TimelineScaleChoice
    {
        public float MillisecondPerPixel { get; set; }

        public float RulerMillisecondSpacing { get; set; }

        public int RulerCountPerTimeTextBlock { get; set; }

        public TimelineScaleChoice() { }

        public TimelineScaleChoice(float MillisecondPerPixel, float RulerMillisecondSpacing, int RulerCountPerTimeTextBlock)
        {
            this.MillisecondPerPixel = MillisecondPerPixel;
            this.RulerMillisecondSpacing = RulerMillisecondSpacing;
            this.RulerCountPerTimeTextBlock = RulerCountPerTimeTextBlock;
        }
    }

    public partial class TimelineModule : LogGenius.Core.Module<TimelineModule>
    {
        [ObservableProperty]
        private Timeline _Timeline = new();

        [ObservableProperty]
        [Setting]
        private List<TimelineScaleChoice> _TimelineScaleChoices = new()
        {
            new(1, 20, 5),
            new(2, 20, 10),
            new(3, 50, 10),
            new(4, 50, 10),
            new(5, 50, 10),
            new(6, 100, 5),
            new(7, 100, 5),
            new(8, 100, 10),
            new(9, 100, 10),
            new(10, 100, 10),
            new(20, 500, 5),
            new(100, 1000, 10),
            new(200, 3000, 20),
            new(500, 6000, 10),
            new(1000, 12000, 20),
        };

        public TimelineModule(Session Session) : base(Session)
        {
            Session.EntriesAdded += OnEntriesAdded;
            Session.EntriesCleared += OnEntriesCleared;
            Session.EntryCreated += OnEntryCreated;
        }

        ~TimelineModule()
        {
            Session.EntriesAdded -= OnEntriesAdded;
            Session.EntriesCleared -= OnEntriesCleared;
            Session.EntryCreated -= OnEntryCreated;
        }

        private void OnEntriesAdded(List<Entry> Entries)
        {
            foreach (var Entry in Entries)
            {
                var HeaderInfo = Entry.GetHeaderInfo();
                if (HeaderInfo == null)
                {
                    continue;
                }
                Timeline.UpdateTime(HeaderInfo.DateTime);
                var RecordLookups = Entry.GetTimelineRecords();
                if (RecordLookups == null || RecordLookups.Count == 0)
                {
                    continue;
                }
                foreach (var (Name, Records) in RecordLookups)
                {
                    foreach (var Record in Records)
                    {
                        Timeline.AddRecord(Name, HeaderInfo.DateTime, Record);
                    }
                }
            }
        }

        private void OnEntriesCleared()
        {
            Timeline = new();
        }

        private void OnEntryCreated(Entry Entry)
        {
            Entry.GetHeaderInfo();
            Entry.GetTimelineRecords();
        }
    }
}