using System.Windows;
using System.Windows.Controls;

namespace LogGenius.Core
{
    public partial class Frame : ContentControl
    {
        public static readonly DependencyProperty WindowActiveProperty =
            DependencyProperty.Register(
                nameof(WindowActive),
                typeof(bool),
                typeof(Frame),
                new PropertyMetadata(false));

        public bool WindowActive
        {
            get => (bool)GetValue(WindowActiveProperty);
            set => SetValue(WindowActiveProperty, value);
        }

        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register(
                nameof(WindowTitle),
                typeof(string),
                typeof(Frame),
                new PropertyMetadata(null));

        public string WindowTitle
        {
            get => (string)GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        protected Menu? Menu { get; private set; }

        protected List<MenuItem>? MenuItems;

        public Frame()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.Menu = GetTemplateChild("PART_Menu") as Menu;
            if (this.Menu != null && MenuItems != null)
            {
                foreach (var MenuItem in MenuItems)
                {
                    this.Menu.Items.Add(MenuItem);
                }
                MenuItems = null;
            }
        }

        public void AddMenuItem(MenuItem MenuItem)
        {
            if (this.Menu == null)
            {
                MenuItems ??= new();
                MenuItems.Add(MenuItem);
            }
            else
            {
                this.Menu.Items.Add(MenuItem);
            }
        }
    }
}
