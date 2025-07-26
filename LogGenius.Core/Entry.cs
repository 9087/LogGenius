namespace LogGenius.Core
{
    public class Entry
    {
        public string Text { get; set; } = string.Empty;

        public uint Line { get; set; }

        public Entry()
        {
        }

        public Entry(string InText)
        {
            Text = InText;
        }

        public Entry(string InText, uint InLine)
        {
            Text = InText;
            Line = InLine;
        }
    }
}
