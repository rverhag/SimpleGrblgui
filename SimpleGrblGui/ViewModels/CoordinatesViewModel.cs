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
        private Grbl grbl;

        public CoordinatesViewModel()
        {
            grbl = Grbl.Interface;
            grbl.CoordinateChanged += Grbl_CoordinateChanged;
            
            grbl.StateChanged += Grbl_StateChanged;
            grbl.FeedRateChanged += Grbl_FeedRateChanged;
            grbl.CurrentWorkCoordinateChanged += Grbl_CurrentWorkCoordinateChanged;
            grbl.SpindleStateChanged += Grbl_SpindleStateChanged;
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
        }

        private void Grbl_CoordinateChanged(object sender, EventArgs e)
        {
            Coordinate coordinate = (Coordinate)sender;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(coordinate.Type));
        }

        public string FeedRate { get { return string.Format("Feed {0} mm/min",grbl.FeedRate); } }

        public Coordinate WCO { get { return grbl.WCO; } }
        public Coordinate MPOS { get { return grbl.MPOS; } }
        public Coordinate WPOS { get { return grbl.WPOS; } }

        public ICommand GoToWorkZero { get { return new DelegatingCommand(grbl.GoToWorkZero); } }
        public ICommand RunHomingcycle { get { return new DelegatingCommand(grbl.RunHomingcycle); } }

        public ICommand SetXWorkToZero { get { return new DelegatingCommand(grbl.SetXWorkToZero); } }
        public ICommand SetYWorkToZero { get { return new DelegatingCommand(grbl.SetYWorkToZero); } }
        public ICommand SetZWorkToZero { get { return new DelegatingCommand(grbl.SetZWorkToZero); } }

        public ICommand SetCorrectedXWorkToZero { get { return new DelegatingCommand(grbl.SetCorrectedXWorkToZero); } }
        public ICommand SetCorrectedYWorkToZero { get { return new DelegatingCommand(grbl.SetCorrectedYWorkToZero); } }
        public ICommand SetCorrectedZWorkToZero { get { return new DelegatingCommand(grbl.SetCorrectedZWorkToZero); } }

        public ICommand IncreaseFeed1 { get { return new DelegatingCommand(grbl.IncreaseFeed1); } }
        public ICommand IncreaseFeed10 { get { return new DelegatingCommand(grbl.IncreaseFeed10); } }
        public ICommand SetFeed100 { get { return new DelegatingCommand(grbl.SetFeed100); } }
        public ICommand DecreaseFeed1 { get { return new DelegatingCommand(grbl.DecreaseFeed1); } }
        public ICommand DecreaseFeed10 { get { return new DelegatingCommand(grbl.DecreaseFeed10); } }


        public ICommand ToggleSpindle
        {
            get
            {
                if ((grbl.SpindleState == SpindleState.ON) & grbl.InIdleState) return new DelegatingCommand(grbl.StopSpindle);
                else return new DelegatingCommand(grbl.StartSpindle);
            }
        }

        public bool SpindleButtonEnabled
        {
            get
            {
                return grbl.InIdleState;
            }
        }


        public bool ZeroButtonsEnabled
        {
            get
            {
                return grbl.InIdleState;
            }
        }

        public bool HomingCycleEnabled
        {
            get
            {
                return grbl.HomingCycleEnabled && grbl.InIdleState;
            }
        }

        public bool WorkCoordinateButtonsEnabled
        {
            get
            {
                return grbl.State == GrblState.IDLE ? true : false;
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
                return grbl.WorkCoordinates.Current.Name;
            }
            set
            {
                grbl.SendCommand(value);
                grbl.GetParserstate();
            }
        }

        public bool JogButtonsEnabled
        {
            get
            {
                return (grbl.State == GrblState.IDLE | grbl.State== GrblState.JOG)  ? true : false;
            }
        }

        public bool FeedRateButtonsEnabled
        {
            get
            {
                return grbl.State == GrblState.RUN ? true : false;
            }
        }

        public double MaxFeedRate
        {
            get
            {
                return grbl.MaxRate;
            }
        }
    }
}
