using LogGenius.Core;

namespace LogGenius.Modules.Entries
{
    public class EntryMatchingState
    {
        public string FilterPattern = string.Empty;

        public bool IsCaseSensitive = false;

        public bool IsRegex = false;

        public EntryMatchingState()
        {
        }

        public EntryMatchingState(string FilterPattern, bool IsCaseSensitive, bool IsRegex)
        {
            this.FilterPattern = FilterPattern;
            this.IsCaseSensitive = IsCaseSensitive;
            this.IsRegex = IsRegex;
        }
    }

    public class EntryMatchingMetaData
    {
        public EntryMatchingState? EntryMatchingState { get; set; }

        public bool Result { get; set; } = true;

        public bool Test(Entry Entry, EntryMatchingState EntryMatchingState)
        {
            if (this.EntryMatchingState == EntryMatchingState)
            {
                return (bool) Result!;
            }
            this.EntryMatchingState = EntryMatchingState;
            if (string.IsNullOrEmpty(this.EntryMatchingState.FilterPattern))
            {
                Result = false;
            }
            else if (this.EntryMatchingState.IsRegex)
            {
                var Regex = RegexCache.Get(this.EntryMatchingState.FilterPattern, this.EntryMatchingState.IsCaseSensitive);
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
                Result = Entry.Text.Contains(this.EntryMatchingState.FilterPattern, this.EntryMatchingState.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }
            return (bool)Result!;
        }
    }

    public static class EntryMatching
    {
        public static bool Test(this Entry Entry, EntryMatchingState EntryMatchingState)
        {
            return Entry.GetOrAddMetaData<EntryMatchingMetaData>().Test(Entry, EntryMatchingState);
        }
    }
}
