using LogGenius.Core;
using System;

namespace LogGenius.Modules.Entries
{
    public class EntryMatchingMetaData
    {
        public string? FilterPattern { get; set; }

        public bool? IsCaseSensitive { get; set; }
        
        public bool? IsRegex { get; set; }

        public bool? Result { get; set; }

        public bool Test(Entry Entry, string FilterPattern, bool IsCaseSensitive, bool IsRegex)
        {
            if (this.FilterPattern == FilterPattern && this.IsCaseSensitive == IsCaseSensitive && this.IsRegex == IsRegex)
            {
                return (bool) Result!;
            }
            this.FilterPattern = FilterPattern;
            this.IsCaseSensitive = IsCaseSensitive;
            this.IsRegex = IsRegex;
            if (string.IsNullOrEmpty(FilterPattern))
            {
                Result = false;
            }
            else if (IsRegex)
            {
                var Regex = RegexCache.Get(FilterPattern, IsCaseSensitive);
                if (Regex == null)
                {
                    Result = false;
                }
                else
                {
                    Result = Regex.IsMatch(Entry.Text);
                }
            }
            else
            {
                Result = Entry.Text.Contains(FilterPattern, IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }
            return (bool)Result!;
        }
    }

    public static class EntryMatching
    {
        public static bool Test(this Entry Entry, string FilterPattern, bool IsCaseSensitive, bool IsRegex)
        {
            return Entry.GetOrAddMetaData<EntryMatchingMetaData>().Test(Entry, FilterPattern, IsCaseSensitive, IsRegex);
        }
    }
}
