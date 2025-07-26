using LogGenius.Core;
using System;
namespace LogGenius.Modules.Entries
{
    public static class EntryMatching
    {
        public static bool Test(this Entry Entry, string FilterPattern, bool IsCaseSensitive, bool IsRegex)
        {
            if (string.IsNullOrEmpty(FilterPattern))
            {
                return false;
            }
            if (IsRegex)
            {
                var Regex = RegexCache.Get(FilterPattern, IsCaseSensitive);
                if (Regex == null)
                {
                    return false;
                }
                else
                {
                    return Regex.IsMatch(Entry.Text);
                }
            }
            else
            {
                return Entry.Text.Contains(FilterPattern, IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
