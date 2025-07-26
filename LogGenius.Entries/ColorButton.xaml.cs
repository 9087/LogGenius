using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogGenius.Modules.Entries
{
    public partial class ColorButton : UserControl
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(ColorButton),
                new PropertyMetadata(System.Windows.Media.Color.FromRgb(0, 0, 0), OnColorChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private static void OnColorChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
            if (Object is ColorButton ColorButton)
            {
                ColorButton.OnSourceChanged();
            }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ColorButton),
                new PropertyMetadata(null, OnTitleChanged));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static void OnTitleChanged(DependencyObject Object, DependencyPropertyChangedEventArgs EventArgs)
        {
        }

        public ColorButton()
        {
            InitializeComponent();
            this.ColorPicker.ColorChanged += OnTargetChanged;
        }

        private void OnTargetChanged(object Sender, RoutedEventArgs EventArgs)
        {
            this.SetCurrentValue(
                ColorProperty,
                System.Windows.Media.Color.FromRgb(
                    (byte)(this.ColorPicker.Color.RGB_R),
                    (byte)(this.ColorPicker.Color.RGB_G),
                    (byte)(this.ColorPicker.Color.RGB_B)
                )
            );
        }

        private void OnSourceChanged()
        {
            this.ColorPicker.ColorChanged -= OnTargetChanged;
            this.ColorPicker.Color.RGB_R = (double)Color.R;
            this.ColorPicker.Color.RGB_G = (double)Color.G;
            this.ColorPicker.Color.RGB_B = (double)Color.B;
            this.ColorPicker.ColorChanged += OnTargetChanged;
        }
    }
}
