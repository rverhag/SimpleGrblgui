using System;
using System.Windows.Controls;
using Vhr;
using VhR.SimpleGrblGui.ViewModels;

namespace VhR.SimpleGrblGui.Usercontrols
{
    public partial class StateControl : UserControl
    {
        public StateControl()
        {
            InitializeComponent();
            DataContext = new StateViewModel();
        }

        private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //Just to be sure there's a direct action.
            Grbl.Interface.Reset();
        }

        private void GcodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grbl.Interface.Gcode.Reset();
            Grbl.Interface.StartProcessingGcode();
        }
    }
}
