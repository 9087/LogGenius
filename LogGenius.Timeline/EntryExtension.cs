using LogGenius.Core;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogGenius.Modules.Timeline
{
    internal class HeaderInfoMetaData
    {
        public HeaderInfo? HeaderInfo { get; }

        public HeaderInfoMetaData(HeaderInfo? HeaderInfo)
        {
            this.HeaderInfo = HeaderInfo;
        }
    }

    internal class TimelineRecordMetaData
    {
        public Dictionary<string, List<PropertyRecord>>? PropertyRecords { get; }

        public TimelineRecordMetaData(Dictionary<string, List<PropertyRecord>>? propertyRecords)
        {
            PropertyRecords = propertyRecords;
        }
    }

    internal static class EntryExtension
    {
        private static Regex HeaderInfoPattern = new Regex(@"^\[(\d\d\d\d\.\d\d\.\d\d-\d\d\.\d\d\.\d\d:\d\d\d)\]\[(\s*\d+)\]");
        private static Regex PropertyRecordPattern = new Regex(@"\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(\d+(?:\.\d+)?)\s*\}");

        public static HeaderInfo? GetHeaderInfo(this Entry Entry)
        {
            var HeaderInfoMetaData = Entry.GetMetaData<HeaderInfoMetaData>();
            if (HeaderInfoMetaData != null)
            {
                return HeaderInfoMetaData.HeaderInfo;
            }

            var Match = HeaderInfoPattern.Match(Entry.Text);
            if (Match.Success)
            {
                var OK = true;
                OK &= System.DateTime.TryParseExact(Match.Groups[1].Value, "yyyy.MM.dd-HH.mm.ss':'fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime DateTime);
                if (OK)
                {
                    double TotalMilliseconds = Math.Round((DateTime - DateTime.MinValue).TotalMilliseconds);
                    DateTime = DateTime.MinValue.AddMilliseconds(TotalMilliseconds);
                }

                OK &= int.TryParse(Match.Groups[2].Value, out int FrameIndex);
                if (OK)
                {
                    var HeaderInfo =  new HeaderInfo(DateTime, FrameIndex);
                    Entry.AddMetaData(new HeaderInfoMetaData(HeaderInfo));
                    return HeaderInfo;
                }
            }
            Entry.AddMetaData(new HeaderInfoMetaData(null));
            return null;
        }

        public static Dictionary<string, List<PropertyRecord>>? GetTimelineRecords(this Entry Entry)
        {
            var TimelineRecordMetaData = Entry.GetMetaData<TimelineRecordMetaData>();
            if (TimelineRecordMetaData != null)
            {
                return TimelineRecordMetaData.PropertyRecords;
            }
            Dictionary<string, List<PropertyRecord>>? RecordLookups = null;
            var Matches = PropertyRecordPattern.Matches(Entry.Text);
            foreach (Match Match in Matches)
            {
                RecordLookups ??= new();
                var Name = Match.Groups[1].Value.Trim();
                if (!double.TryParse(Match.Groups[2].Value.Trim(), out var Value))
                {
                    continue;
                }
                var NewRecord = new PropertyRecord(Entry, Value);
                if (!RecordLookups.ContainsKey(Name))
                {
                    RecordLookups.Add(Name, new());
                }
                RecordLookups[Name].Add(NewRecord);
            }
            Entry.AddMetaData(new TimelineRecordMetaData(RecordLookups));
            return RecordLookups;
        }
    }
}
