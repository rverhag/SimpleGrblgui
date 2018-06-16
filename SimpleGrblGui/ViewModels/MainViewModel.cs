using System.ComponentModel;
using System.Windows;
using Vhr;
using Vhr.Enums;
using Vhr.Events;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Grbl grbl;
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            grbl = Grbl.Interface;
            grbl.StateChanged   += Grbl_StateChanged;
            grbl.ErrorReceived += Grbl_ErrorReceived;

            if (!grbl.Initialized)
            {
                grbl.Initialize();
            }

            SettingsMenuEnabled = (grbl.State == GrblState.IDLE && grbl.Settings.Loaded);
        }

        private void Grbl_ErrorReceived(object sender, ErrorReceivedEventArgs e)
        {
            if (!grbl.GcodeIsRunning)
                MessageBox.Show(e.Error);
        }

        private void Grbl_StateChanged(object sender, System.EventArgs e)
        {
            SettingsMenuEnabled = (grbl.State == GrblState.IDLE && grbl.Settings.Loaded);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SettingsMenuEnabled"));
        }
       
        public bool SettingsMenuEnabled { get; private set; }
    }
}
