using System;
using System.Windows.Controls;
using Vhr;
using VhR.SimpleGrblGui.ViewModels;

namespace VhR.SimpleGrblGui.Usercontrols
{
    public partial class CoordinatesControl : UserControl
    {
        private Grbl grbl;

        public CoordinatesControl()
        {
            InitializeComponent();
            grbl = Grbl.Interface;
            DataContext = new CoordinatesViewModel();
        }

        
        private void ButtonJogCancel(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogCancel();
        }

        private void Left_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogLeft(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }
      
        private void Right_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogRight(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }

        private void Forward_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogForward(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }

        private void Back_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogBack(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }

        private void Up_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogUp(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }

        private void Down_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            grbl.JogDown(string.Format("F{0}", Convert.ToInt32(FeedRate.Value)));
        }
             
             
    }
}
