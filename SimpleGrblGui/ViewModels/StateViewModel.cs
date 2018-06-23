using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Vhr;
using Vhr.Enums;
using VhR.SimpleGrblGui.Classes;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class StateViewModel : INotifyPropertyChanged
    {
        private Grbl grbl;
        public event PropertyChangedEventHandler PropertyChanged;

        public StateViewModel()
        {
            grbl = Grbl.Interface;
            grbl.StateChanged +=  Grbl_StateChanged;
            grbl.GcodeChanged += Grbl_GcodeChanged;
            grbl.GcodeLineChanged += Grbl_GcodeLineChanged;
        }

        private void Grbl_GcodeLineChanged(object sender, System.EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("QueuedSize"));
        }

        private void Grbl_GcodeChanged(object sender, System.EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StartGcodeEnabled"));
        }

        private void Grbl_StateChanged(object sender, System.EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("State"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StateMessage"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActionText"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActionCommand"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActionVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GrblCommandVisibility"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Background"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StartGcodeEnabled"));
        }

        public ICommand ResetCommand { get { return new DelegatingCommand(grbl.Reset); } }
        public ICommand GrblCommand { get { return new DelegatingCommand(Sendcommand); } }
        void Sendcommand(object parameter)
        {
            grbl.SendCommand((string)parameter);
        }
        public Visibility GrblCommandVisibility
        {
            get
            {
                switch (grbl.State)
                {
                    case GrblState.IDLE:
                    case GrblState.CHECK:
                        return Visibility.Visible;
                    default:
                        return Visibility.Hidden;
                }
            }
        }


        public bool StartGcodeEnabled
        {
            get
            {
                if (grbl.Gcode == null)
                    return false;
                else
                    if (grbl.InIdleState || grbl.InCheckState)
                    return true;
                else
                    return false;
            }
        }

        public ICommand ActionCommand
        {
            get
            {
                switch (grbl.State)
                {
                    case GrblState.ALARM:
                        return new DelegatingCommand(grbl.KillAlarmLock);
                    case GrblState.RUN:
                        return new DelegatingCommand(grbl.HoldFeed);
                    case GrblState.IDLE:
                        return new DelegatingCommand(grbl.EnableCheckmode);
                    case GrblState.HOLD0:
                    case GrblState.HOLD1:
                    case GrblState.DOOR0:
                    case GrblState.DOOR1:
                    case GrblState.DOOR2:
                    case GrblState.DOOR3:
                        return new DelegatingCommand(grbl.StartCycleAsync);
                    default:
                        return new DelegatingCommand(grbl.GetStatus);
                }
            }
        }

        public string ActionText
        {
            get
            {
                switch (grbl.State)
                {
                    case GrblState.IDLE:
                        return "CheckMode";
                    case GrblState.ALARM:
                        return "Kill Alarm";
                    case GrblState.RUN:
                        return "Hold Feed";
                    case GrblState.HOLD0:
                    case GrblState.HOLD1:
                    case GrblState.DOOR0:
                    case GrblState.DOOR1:
                    case GrblState.DOOR2:
                    case GrblState.DOOR3:
                        return "Start Cycle";
                    default:
                        return "";
                }
            }
        }

        public Visibility ActionVisibility
        {
            get
            {
                switch (grbl.State)
                {
                    case GrblState.IDLE:
                    case GrblState.ALARM:
                    case GrblState.RUN:
                    case GrblState.HOLD0:
                    case GrblState.HOLD1:
                    case GrblState.DOOR0:
                    case GrblState.DOOR1:
                    case GrblState.DOOR2:
                    case GrblState.DOOR3:
                        return Visibility.Visible;
                    default:
                        return Visibility.Hidden;
                }
            }
        }
        public GrblState State { get { return grbl.State; } }
        public string StateMessage { get { return grbl.StateMessage; } }
        public int QueuedSize { get { return grbl.QueuedSize; } }
        
        public SolidColorBrush Background
        {
            get
            {
                switch (grbl.State)
                {
                    case GrblState.IDLE:
                        return Brushes.Transparent;
                    case GrblState.CHECK:
                        return Brushes.Green;
                    case GrblState.ALARM:
                        return Brushes.OrangeRed;
                    case GrblState.RUN:
                    case GrblState.JOG:
                        return Brushes.Red;
                    case GrblState.HOLD0:
                    case GrblState.HOLD1:
                    case GrblState.DOOR0:
                    case GrblState.DOOR1:
                    case GrblState.DOOR2:
                    case GrblState.DOOR3:
                        return Brushes.Orange;
                    default:
                        return Brushes.Transparent;
                }
            }
        }
    }
}
