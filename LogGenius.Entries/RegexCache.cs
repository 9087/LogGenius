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
