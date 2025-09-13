namespace LogGenius.Modules.Timeline
{
    public class PropertyIdentity
    {
        public string Name { get; }

        public double? Lower { get; private set; }

        public double? Upper { get; private set; }

        public PropertyIdentity(string Name)
        {
            this.Name = Name;
        }

        public override string ToString()
        {
            return $"{Name}";
        }

        public bool UpdateBound(PropertyRecord Record)
        {
            bool Updated = false;
            if (Lower == null || Record.Value < Lower)
            {
                Lower = Record.Value;
                Updated = true;
            }
            if (Upper == null || Record.Value > Upper)
            {
                Upper = Record.Value;
                Updated = true;
            }
            return Updated;
        }
    }
}
