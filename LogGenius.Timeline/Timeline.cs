using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LogGenius.Modules.Timeline
{
    public partial class Timeline : ObservableObject
    {
        private List<PropertyIdentity> Identities = new();
        private Dictionary<PropertyIdentity, Section> SectionLookupTable = new();

        [ObservableProperty]
        private ObservableCollection<Section> _Sections = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Duration))]
        [NotifyPropertyChangedFor(nameof(TotalMilliseconds))]
        private DateTime? _InitialTime = null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Duration))]
        [NotifyPropertyChangedFor(nameof(TotalMilliseconds))]
        private DateTime? _CurrentTime;

        public TimeSpan Duration => CurrentTime - InitialTime ?? new TimeSpan(0);

        public double TotalMilliseconds => Duration.TotalMilliseconds;

        static float[] MillisecondPerPixelChoices = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 50, 100, 200, 500, 1000 };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MillisecondPerPixel))]
        private int _MillisecondPerPixelIndex = 1;

        public float MillisecondPerPixel => MillisecondPerPixelChoices[MillisecondPerPixelIndex];

        public PropertyIdentity? FindIdentity(string Name)
        {
            return Identities.FirstOrDefault(X => X.Name == Name);
        }

        public void AddIdentity(PropertyIdentity Identity)
        {
            Identities.Add(Identity);
            var NewSection = new Section(Identity);
            SectionLookupTable.Add(Identity, NewSection);
            Sections.Add(NewSection);
        }

        public PropertyIdentity FindOrAddIdentity(string Name)
        {
            var Found = FindIdentity(Name);
            if (Found == null)
            {
                AddIdentity(Found = new(Name));
            }
            return Found;
        }

        public void AddRecord(DateTime DateTime, PropertyRecord Record)
        {
            if (!SectionLookupTable.ContainsKey(Record.Identity))
            {
                AddIdentity(Record.Identity);
            }
            Record.Identity.UpdateBound(Record);
            this.SectionLookupTable[Record.Identity].AddRecord(DateTime, Record);
        }

        public void UpdateTime(DateTime Time)
        {
            if (InitialTime == null)
            {
                InitialTime = Time;
            }
            CurrentTime = Time;
        }
    }
}
