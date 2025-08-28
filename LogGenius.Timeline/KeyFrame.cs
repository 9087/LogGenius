using CommunityToolkit.Mvvm.ComponentModel;

namespace LogGenius.Modules.Timeline
{
    public partial class KeyFrame : ObservableObject
    {
        [ObservableProperty]
        private List<PropertyRecord> _Records = new();

        public DateTime DateTime { get; }

        public KeyFrame(DateTime DateTime)
        {
            this.DateTime = DateTime;
        }

        public void AddRecord(PropertyRecord Record)
        {
            Records.Add(Record);
        }
    }
}
