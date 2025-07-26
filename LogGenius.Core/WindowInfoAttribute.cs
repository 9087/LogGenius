using System.Reflection;

namespace LogGenius.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class WindowInfoAttribute : Attribute
    {
        public string MenuTitle { get; }

        public int MenuOrder { get; } = 0;

        public bool ShouldBeOpenedOnStartup { get; } = false;

        public WindowInfoAttribute(string MenuTitle, int MenuOrder = 0, bool ShouldBeOpenedOnStartup = false)
        {
            this.MenuTitle = MenuTitle;
            this.MenuOrder = MenuOrder;
            this.ShouldBeOpenedOnStartup = ShouldBeOpenedOnStartup;
        }

        internal static string GetMenuTitle(Type WindowType)
        {
            return WindowType.GetCustomAttribute<WindowInfoAttribute>()?.MenuTitle ?? WindowType.Name;
        }

        internal static int GetMenuOrder(Type WindowType)
        {
            return WindowType.GetCustomAttribute<WindowInfoAttribute>()?.MenuOrder ?? 0;
        }

        internal static bool GetShouldBeOpenedOnStartup(Type WindowType)
        {
            return WindowType.GetCustomAttribute<WindowInfoAttribute>()?.ShouldBeOpenedOnStartup ?? false;
        }
    }
}
