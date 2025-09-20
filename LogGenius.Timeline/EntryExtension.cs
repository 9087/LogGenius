using LogGenius.Core;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LogGenius.Modules.Timeline
{
    internal static class EntryExtension
    {
        private static Regex HeaderInfoPattern = new Regex(@"^\[(\d\d\d\d\.\d\d\.\d\d-\d\d\.\d\d\.\d\d:\d\d\d)\]\[(\s*\d+)\]");
        private static Regex PropertyRecordPattern = new Regex(@"\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(\d+(?:\.\d+)?)\s*\}");

        public static HeaderInfo? GetHeaderInfo(this Entry Entry)
        {
            var Match = HeaderInfoPattern.Match(Entry.Text);
            if (!Match.Success)
            {
                return null;
            }
            var OK = true;
            OK &= System.DateTime.TryParseExact(Match.Groups[1].Value, "yyyy.MM.dd-HH.mm.ss':'fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime DateTime);
            if (OK)
            {
                double TotalMilliseconds = Math.Round((DateTime - DateTime.MinValue).TotalMilliseconds);
                DateTime = DateTime.MinValue.AddMilliseconds(TotalMilliseconds);
            }

            OK &= int.TryParse(Match.Groups[2].Value, out int FrameIndex);
            if (!OK)
            {
                return null;
            }
            return new HeaderInfo(DateTime, FrameIndex);
        }

        public static List<PropertyRecord>? GetRecords(this Entry Entry, Timeline Timeline, Dictionary<string, PropertyIdentity> NewIdentities)
        {
            List<PropertyRecord>? Records = null;
            var Matches = PropertyRecordPattern.Matches(Entry.Text);
            foreach (Match Match in Matches)
            {
                Records ??= new();
                var Name = Match.Groups[1].Value.Trim();
                if (!double.TryParse(Match.Groups[2].Value.Trim(), out var Value))
                {
                    continue;
                }
                var Identity = Timeline.FindIdentity(Name);
                if (Identity == null)
                {
                    if (!NewIdentities.TryGetValue(Name, out Identity))
                    {
                        Identity = new PropertyIdentity(Name);
                        NewIdentities.Add(Name, Identity);
                    }
                }
                var NewRecord = new PropertyRecord(
                    Entry,
                    Identity,
                    Value
                );
                Records.Add(NewRecord);
            }
            return Records;
        }
    }
}
