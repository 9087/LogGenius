using System.Text.RegularExpressions;

namespace LogGenius.Modules.Entries
{
    public class RegexCache
    {
        public static RegexCache Cache { get; } = new();

        public string Pattern { get; private set; }

        public bool CaseSensitiveEnabled { get; private set; }

        public Regex? Regex { get; private set; }

        public RegexCache()
        {
            Pattern = string.Empty;
            CaseSensitiveEnabled = false;
            Regex = new(Pattern);
        }

        public static Regex? Get(string Pattern, bool CaseSensitiveEnabled)
        {
            if (Cache.Pattern != Pattern || Cache.CaseSensitiveEnabled != CaseSensitiveEnabled)
            {
                Cache.Pattern = Pattern;
                Cache.CaseSensitiveEnabled = CaseSensitiveEnabled;
                try
                {
                    int StartIndex = 0;
                    int EndIndex = Pattern.Length;
                    while (StartIndex < EndIndex && Pattern[StartIndex] == '|')
                    {
                        StartIndex++;
                    }
                    while (StartIndex < EndIndex && Pattern[EndIndex - 1] == '|' && Pattern[EndIndex - 2] != '\\')
                    {
                        EndIndex--;
                    }
                    Pattern = Pattern.Substring(StartIndex, EndIndex - StartIndex);
                    Cache.Regex = new(Pattern, CaseSensitiveEnabled ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch (System.Text.RegularExpressions.RegexParseException)
                {
                    Cache.Regex = null;
                }
            }
            return Cache.Regex;
        }
    }
}
