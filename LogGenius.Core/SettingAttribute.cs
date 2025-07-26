namespace LogGenius.Core
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SettingAttribute : Attribute
    {
    }
}
