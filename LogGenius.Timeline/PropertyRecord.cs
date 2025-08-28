using LogGenius.Core;

namespace LogGenius.Modules.Timeline
{
    public class PropertyRecord
    {
        public Entry Entry { get; }

        public PropertyIdentity Identity { get; }

        public double Value { get; }

        public PropertyRecord(Entry Entry, PropertyIdentity Identity, double Value)
        {
            this.Entry = Entry;
            this.Identity = Identity;
            this.Value = Value;
        }

        public override string ToString()
        {
            return $"{Identity}{Value}";
        }
    }
}