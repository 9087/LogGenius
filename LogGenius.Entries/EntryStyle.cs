using CommunityToolkit.Mvvm.ComponentModel;
using LogGenius.Core;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LogGenius.Modules.Entries
{
    public partial class EntryStyle : ObservableObject
    {
        public static EntryStyle Default { get; } = new EntryStyle();

        [ObservableProperty]
        protected string _FilterPattern = string.Empty;

        [ObservableProperty]
        private bool _IsCaseSensitive = false;

        [ObservableProperty]
        private bool _IsRegex = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Foreground))]
        private Color _ForegroundColor = Color.FromRgb(0, 0, 0);

        public Brush Foreground => new SolidColorBrush(ForegroundColor);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Background))]
        private Color _BackgroundColor = Color.FromRgb(255, 255, 255);

        public Brush Background => new SolidColorBrush(BackgroundColor);

        [ObservableProperty]
        private FontWeight _FontWeight = new FontWeight();

        public bool Test(Entry Entry)
        {
            return Entry.Test(FilterPattern, IsCaseSensitive, IsRegex);
        }
    }

    public abstract class GetEntryStyleFromEntry : IMultiValueConverter
    {
        public EntryStyle ConvertToEntryStyle(object[] Values, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Values.Length != 2)
            {
                throw new ArgumentException();
            }
            if (!(Values[0] is Entry Entry))
            {
                throw new ArgumentException();
            }
            if (!(Values[1] is ObservableCollection<EntryStyle> EntryStyles))
            {
                throw new ArgumentException();
            }
            return EntryStyles.FirstOrDefault(X => X.Test(Entry), EntryStyle.Default);
        }

        public object[] ConvertBack(object Value, Type[] TargetTypes, object Parameter, CultureInfo Culture)
        {
            throw new NotSupportedException();
        }

        public abstract object Convert(object[] Values, Type TargetType, object Parameter, CultureInfo Culture);
    }

    public class GetEntryStyleForegroundFromEntry : GetEntryStyleFromEntry
    {
        public override object Convert(object[] Values, Type TargetType, object Parameter, CultureInfo Culture)
        {
            return ConvertToEntryStyle(Values, TargetType, Parameter, Culture).Foreground;
        }
    }

    public class GetEntryStyleBackgroundFromEntry : GetEntryStyleFromEntry
    {
        public override object Convert(object[] Values, Type TargetType, object Parameter, CultureInfo Culture)
        {
            return ConvertToEntryStyle(Values, TargetType, Parameter, Culture).Background;
        }
    }
}
