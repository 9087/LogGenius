using System.Text;

namespace LogGenius.Core
{
    public class EntryGenerator
    {
        private StringBuilder LineBuilder = new();

        public static char CR = '\r';
        public static char LF = '\n';

        public bool LastFinished => LineBuilder.Length == 0;

        public List<Entry> Push(Memory<char> Buffer, int Length)
        {
            List<Entry> Entries = new();
            for (int Index = 0; Index < Length; Index++)
            {
                bool NewLine = false;
                if (LineBuilder.Length > 0)
                {
                    int LastIndex = LineBuilder.Length - 1;
                    if (LineBuilder[LastIndex] == LF)
                    {
                        LineBuilder.Remove(LastIndex, 1);
                        NewLine = true;
                    }
                    else if (LineBuilder[LastIndex] == CR)
                    {
                        LineBuilder.Remove(LastIndex, 1);
                        NewLine = true;
                        if (Buffer.Span[Index] == LF)
                        {
                            Index++;
                        }
                    }
                }
                if (NewLine)
                {
                    Entries.Add(new(LineBuilder.ToString()));
                    LineBuilder.Clear();
                }
                if (Index < Length)
                {
                    LineBuilder.Append(Buffer.Span[Index]);
                }
            }
            if (!LastFinished)
            {
                Entries.Add(new(LineBuilder.ToString()));
            }
            return Entries;
        }

        public void Clear()
        {
            LineBuilder.Clear();
        }
    }
}
