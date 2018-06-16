using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VhR.SimpleGrblGui.Usercontrols
{
    //See https://www.codeproject.com/Articles/140521/Color-Picker-using-WPF-Combobox
    public partial class ColorPickerControl : UserControl
    {
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        public ColorPickerControl()
        {
            InitializeComponent();
        }

        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  
        // This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor",
        typeof(Brush), typeof(ColorPickerControl), new UIPropertyMetadata(null));

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}

