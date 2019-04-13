using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using Vhr;
using Vhr.Enums;
using Vhr.Types;
using VhR.SimpleGrblGui.Classes;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class CoordinatesViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public CoordinatesViewModel()
        {
            App.Grbl.CoordinateChanged += Grbl_CoordinateChanged;
            App.Grbl.StateChanged += Grbl_StateChanged;
            App.Grbl.FeedRateChanged += Grbl_FeedRateChanged;
            App.Grbl.CurrentWorkCoordinateChanged += Grbl_CurrentWorkCoordinateChanged;
            App.Grbl.SpindleStateChanged += Grbl_SpindleStateChanged;
            App.Grbl.CoolingStateChanged += Grbl_CoolingStateChanged;
        }

        private void Grbl_CoolingStateChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToggleCooling"));
        }

        private void Grbl_SpindleStateChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ToggleSpindle"));
        }

        private void Grbl_FeedRateChanged(object sender, Vhr.Events.FeedRateChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FeedRate"));
        }

        private void Grbl_StateChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ZeroButtonsEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("JogButtonsEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FeedRateButtonsEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WorkCoordinateButtonsEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxFeedRate"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HomingCycleEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpindleButtonEnabled"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CoolingButtonEnabled"));
        }

        private void Grbl_CoordinateChanged(object sender, EventArgs e)
        {
            Coordinate coordinate = (Coordinate)sender;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(coordinate.Type));
        }

        public string FeedRate { get { return string.Format("Feed {0} mm/min",App.Grbl.FeedRate); } }

        public Coordinate WCO { get { return App.Grbl.WCO; } }
        public Coordinate MPOS { get { return App.Grbl.MPOS; } }
        public Coordinate WPOS { get { return App.Grbl.WPOS; } }

        public ICommand GoToWorkZero { get { return new DelegatingCommand(App.Grbl.GoToWorkZero); } }
        public ICommand RunHomingcycle { get { return new DelegatingCommand(App.Grbl.RunHomingcycle); } }

        public ICommand SetXWorkToZero { get { return new DelegatingCommand(App.Grbl.SetXWorkToZero); } }
        public ICommand SetYWorkToZero { get { return new DelegatingCommand(App.Grbl.SetYWorkToZero); } }
        public ICommand SetZWorkToZero { get { return new DelegatingCommand(App.Grbl.SetZWorkToZero); } }

        public ICommand SetCorrectedXWorkToZero { get { return new DelegatingCommand(App.Grbl.SetCorrectedXWorkToZero); } }
        public ICommand SetCorrectedYWorkToZero { get { return new DelegatingCommand(App.Grbl.SetCorrectedYWorkToZero); } }
        public ICommand SetCorrectedZWorkToZero { get { return new DelegatingCommand(App.Grbl.SetCorrectedZWorkToZero); } }

        public ICommand IncreaseFeed1 { get { return new DelegatingCommand(App.Grbl.IncreaseFeed1); } }
        public ICommand IncreaseFeed10 { get { return new DelegatingCommand(App.Grbl.IncreaseFeed10); } }
        public ICommand SetFeed100 { get { return new DelegatingCommand(App.Grbl.SetFeed100); } }
        public ICommand DecreaseFeed1 { get { return new DelegatingCommand(App.Grbl.DecreaseFeed1); } }
        public ICommand DecreaseFeed10 { get { return new DelegatingCommand(App.Grbl.DecreaseFeed10); } }


        public ICommand ToggleSpindle
        {
            get
            {
                if ((App.Grbl.SpindleState == SpindleState.ON) & App.Grbl.InIdleState) return new DelegatingCommand(App.Grbl.StopSpindle);
                else return new DelegatingCommand(App.Grbl.StartSpindle);
            }
        }

        public ICommand ToggleCooling
        {
            get
            {
                if (App.Grbl.CoolingState == CoolingState.FLOOD_ON | App.Grbl.CoolingState == CoolingState.MIST_ON)
                {
                    return new DelegatingCommand(App.Grbl.StopCooling);
                }
                else
                {
                    return new DelegatingCommand(App.Grbl.StartCooling);
                }
            }
        }

        public bool SpindleButtonEnabled
        {
            get
            {
                return App.Grbl.InIdleState;
            }
        }

        public bool CoolingButtonEnabled
        {
            get
            {
                return App.Grbl.InIdleState;
            }
        }

        public bool ZeroButtonsEnabled
        {
            get
            {
                return App.Grbl.InIdleState;
            }
        }

        public bool HomingCycleEnabled
        {
            get
            {
                return App.Grbl.HomingCycleEnabled && App.Grbl.InIdleState;
            }
        }

        public bool WorkCoordinateButtonsEnabled
        {
            get
            {
                return App.Grbl.State == GrblState.IDLE ? true : false;
            }
        }

        private void Grbl_CurrentWorkCoordinateChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentWorkCoordinate"));
        }

        public string CurrentWorkCoordinate
        {
            get
            {
                return App.Grbl.WorkCoordinates.Current.Name;
            }
            set
            {
                App.Grbl.SendCommand(value);
                App.Grbl.GetParserstate();
            }
        }

        public bool JogButtonsEnabled
        {
            get
            {
                return (App.Grbl.State == GrblState.IDLE | App.Grbl.State== GrblState.JOG)  ? true : false;
            }
        }

        public bool FeedRateButtonsEnabled
        {
            get
            {
                return App.Grbl.State == GrblState.RUN ? true : false;
            }
        }

        public double MaxFeedRate
        {
            get
            {
                return App.Grbl.MaxRate;
            }
        }
    }
}
