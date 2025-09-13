using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LogGenius.Modules.Timeline
{
    public partial class Section : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<KeyFrame> _KeyFrames = new();

        public Action<PropertyRecord>? RecordAdded { get; set; }

        public PropertyIdentity Identity { get; }

        [ObservableProperty]
        private double _HeaderHeight = 60.0;

        public double MinHeaderHeight => 30.0;

        public double MaxHeaderHeight => 300.0;

        public Section(PropertyIdentity Identity)
        {
            this.Identity = Identity;
        }

        public void AddRecord(DateTime DateTime, PropertyRecord Record)
        {
            var LastKeyFrame = KeyFrames.LastOrDefault();
            if (LastKeyFrame == null || LastKeyFrame.DateTime != DateTime)
            {
                KeyFrames.Add(LastKeyFrame = new(DateTime));
            }
            LastKeyFrame.AddRecord(Record);
            RecordAdded?.Invoke(Record);
        }
    }
}
