using System.Collections.ObjectModel;
using System.ComponentModel;
using Vhr;
using Vhr.Types;

namespace VhR.SimpleGrblGui.ViewModels
{
    class SettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private Grbl grbl;
        public SettingsViewModel()
        {
            grbl = Grbl.Interface;
            FillSettings();
        }

        private void FillSettings()
        {
            Settings = new ObservableCollection<Setting>();

            foreach (Setting setting in grbl.Settings.Values)
            {
                Settings.Add(setting);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Settings"));
        }

        public ObservableCollection<Setting> Settings { get; private set; }
        
    }
}
