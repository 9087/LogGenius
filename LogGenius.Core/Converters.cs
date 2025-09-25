using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogGenius.Core
{
    public class GetVisibilityFromBoolean : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value is bool Boolean)
            {
                return Boolean ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }
}
