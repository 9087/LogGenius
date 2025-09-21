using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MillisecondPerPixel))]
        [NotifyPropertyChangedFor(nameof(RulerMillisecondSpacing))]
        [NotifyPropertyChangedFor(nameof(RulerCountPerTimeTextBlock))]
        private int _MillisecondPerPixelIndex = 1;

        public float MillisecondPerPixel => TimelineModule.Instance.TimelineScaleChoices[MillisecondPerPixelIndex - 1].MillisecondPerPixel;

        public float RulerMillisecondSpacing => TimelineModule.Instance.TimelineScaleChoices[MillisecondPerPixelIndex - 1].RulerMillisecondSpacing;

        public int RulerCountPerTimeTextBlock => TimelineModule.Instance.TimelineScaleChoices[MillisecondPerPixelIndex - 1].RulerCountPerTimeTextBlock;

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

        public double GetHorizontalByMillisecond(double Millisecond, double Offset)
        {
            Debug.Assert(InitialTime != null);
            return -Offset + Millisecond / this.MillisecondPerPixel;
        }

        public double GetHorizontalByTime(DateTime Time, double Offset)
        {
            Debug.Assert(InitialTime != null);
            return GetHorizontalByMillisecond((Time - (DateTime)InitialTime).TotalMilliseconds, Offset);
        }

        public double GetMillisecondByHorizontal(double Horizontal, double Offset)
        {
            Debug.Assert(InitialTime != null);
            return (Horizontal + Offset) * this.MillisecondPerPixel;
        }

        public DateTime? GetTimeByHorizontal(double Horizontal, double Offset)
        {
            Debug.Assert(InitialTime != null);
            return ((DateTime) InitialTime).AddMilliseconds(GetMillisecondByHorizontal(Horizontal, Offset));
        }
    }
}
