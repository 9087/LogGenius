namespace LogGenius.Modules.Timeline
{
    internal class HeaderInfo
    {
        public DateTime DateTime { get; }

        public int FrameIndex { get; }

        public HeaderInfo(DateTime DateTime, int FrameIndex)
        {
            this.DateTime = DateTime;
            this.FrameIndex = FrameIndex;
        }
    }
}
