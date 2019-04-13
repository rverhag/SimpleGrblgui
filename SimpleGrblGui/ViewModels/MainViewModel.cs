using System.ComponentModel;
using System.Windows;
using Vhr;
using Vhr.Enums;
using Vhr.Events;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            App.Grbl.StateChanged   += Grbl_StateChanged;
            App.Grbl.ErrorReceived += Grbl_ErrorReceived;
        }

        private void Grbl_ErrorReceived(object sender, ErrorReceivedEventArgs e)
        {
            if (!App.Grbl.GcodeIsRunning)
                MessageBox.Show(e.Error);
        }

        private void Grbl_StateChanged(object sender, System.EventArgs e)
        {
            SettingsMenuEnabled = (App.Grbl.State == GrblState.IDLE && App.Grbl.Settings.Loaded);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SettingsMenuEnabled"));
        }
       
        public bool SettingsMenuEnabled { get; private set; }
    }
}
