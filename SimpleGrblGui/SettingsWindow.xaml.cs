using System.Windows;
using VhR.SimpleGrblGui.ViewModels;
using Vhr;
using Vhr.Types;

namespace VhR.SimpleGrblGui
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
