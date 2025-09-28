using LogGenius.Core;

namespace LogGenius.Modules.Timeline
{
    public class PropertyRecord
    {
        public Entry Entry { get; }

        public PropertyIdentity? _Identity = null;

        public PropertyIdentity Identity => _Identity!;

        public double Value { get; }

        public PropertyRecord(Entry Entry, double Value)
        {
            this.Entry = Entry;
            this.Value = Value;
        }

        public override string ToString()
        {
            return $"{Identity}{Value}";
        }

        public void SetIdentity(PropertyIdentity Identity)
        {
            this._Identity = Identity;
        }
    }
}