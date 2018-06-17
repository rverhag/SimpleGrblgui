using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vhr.Codes;
using Vhr.Enums;
using Vhr.Events;
using Vhr.Gcode;
using Vhr.Types;

namespace Vhr
{
    
    public sealed class Grbl : IDisposable
    {
        #region events
        public event EventHandler StateChanged;
        public event EventHandler IsReset;
        public event EventHandler SpindleStateChanged;
        public event EventHandler CoolingStateChanged;
        public event EventHandler GcodeLineChanged;
        public event EventHandler GcodeChanged;
        public event EventHandler CoordinateChanged;
        public event EventHandler CurrentWorkCoordinateChanged;
        public event EventHandler<ErrorReceivedEventArgs> ErrorReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<FeedRateChangedEventArgs> FeedRateChanged;
        #endregion

        #region grbl-instance related
        private static readonly Lazy<Grbl> grblinterface = new Lazy<Grbl>(() => new Grbl());
        public static Grbl Interface
        {
            get
            {
                return grblinterface.Value;
            }
        }

        private Grbl()
        {
            IntializeSerialPort();

            pollinginterval = Convert.ToInt16(ConfigurationManager.AppSettings["PollingInterval"]);

            WCO.CoordinateChanged += WCO_CoordinateChanged;
            WPOS.CoordinateChanged += WPOS_CoordinateChanged;
            MPOS.CoordinateChanged += MPOS_CoordinateChanged;

            Reset();

            Initialize();
        }

        public bool Initialized { get; set; } = false;
        public void Initialize()
        {
            Thread.Sleep(300);
            GetSettings();
            GetParserstate();
            GetGcodeParameters();

            Thread.Sleep(300);
            Initialized = Settings.Loaded;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FinalizeSerialPort();
                }
                FinalizeSerialPort();

                disposedValue = true;
            }
        }

        ~Grbl()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion

        #region Serial port related
        private SerialPort serialport = null;

        private void FinalizeSerialPort()
        {
            if (serialport != null && serialport.IsOpen) serialport.Close();

            if (serialport != null)
            {
                serialport.DataReceived -= Serialport_DataReceived;
                serialport.Dispose();
                serialport = null;
            }
        }

        private void IntializeSerialPort()
        {
            if (serialport == null)
            {
                serialport = new SerialPort
                {
                    PortName = Convert.ToString(ConfigurationManager.AppSettings["ComPort"]),

                    //Default encoding is ASCII-Encoding, but this is an 7 bit encoding. 
                    //We need an 8 bit encoding and we'll go for ISO 8859-1
                    Encoding = Encoding.GetEncoding(28591),

                    BaudRate = Convert.ToInt32(ConfigurationManager.AppSettings["BaudRate"]),
                    ReadBufferSize = Convert.ToInt32(ConfigurationManager.AppSettings["ReadBufferSize"]),
                    WriteBufferSize = Convert.ToInt32(ConfigurationManager.AppSettings["WriteBufferSize"]),
                    ReceivedBytesThreshold = Convert.ToInt32(ConfigurationManager.AppSettings["ReceivedBytesThreshold"]),
                    DiscardNull = true
                };

                serialport.DataReceived += Serialport_DataReceived;

                if (!serialport.IsOpen) serialport.Open();
                if (serialport.IsOpen)
                {
                    serialport.DiscardInBuffer();
                    serialport.DiscardOutBuffer();
                }
            }
        }

        private void Serialport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (serialport) while (serialport.BytesToRead > 0) ProcessDataReceived(serialport.ReadLine());
        }

        private void ProcessDataReceived(string _data)
        {
            string data = _data.Replace("\r", "");
           // Debug.WriteLine(data);
            char firstletter = !string.IsNullOrEmpty(data) ? data.ToUpper().ToCharArray()[0] : (char)32;

            switch (firstletter)
            {
                case 'O'://ok
                    ProcessResponseAsync(data);
                    break;
                case 'E'://it's an error, but still a response 
                    ProcessResponseAsync(data, true);
                    break;
                case '<': //It's a statusreport
                    {
                        string[] fields = data.ToUpper().Substring(1, data.IndexOf(">") - 1).Split('|');
                        ProcessState(fields[0]); //Fields[0] is allways the state

                        //are there more fields?
                        if (fields.Length > 1)
                        {
                            for (int i = fields.Length - 1; i > 0; i--)
                            {
                                string[] key = fields[i].Split(':');
                                switch (key[0])
                                {
                                    case "WCO":
                                        ProcessWCO(key[1]);
                                        break;
                                    case "MPOS":
                                        ProcessMPOS(key[1]);
                                        break;
                                    case "WPOS":
                                        ProcessWPOS(key[1]);
                                        break;
                                    case "BF":
                                        //Not interested in buffersize or blocks available. We use the "counting bytes" method to keep
                                        //the serial buffer filled
                                        break;
                                    case "FS":
                                        ProcessFeedAndSpindle(key[1]);
                                        break;
                                }
                            }
                        }
                    }
                    break;
                case 'G': // it's a welcome-message.
                    GetStatus();
                    break;
                case 'A':  //it's an alarm
                    ProcessState("ALARM", Alarm.Codes[data.Split(':')[1]]);
                    break;
                case '[':  //it's a feedback
                    if (data.Contains("[G5")) ProcessGcodeParameter(data);
                    else if (data.Contains("[MSG:")) ProcessFeedback(data);
                    else if (data.Contains("[GC:")) ProcessParserState(data);
                    break;
                case '$': // setting or startupline
                    if (!data.StartsWith("$N")) ProcessSetting(data);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Response
        private async Task ProcessResponseAsync(string _data, bool _iserror = false)
        {
            if (GcodeIsRunning & (nextindexforbuffer < Gcode?.Count))
            {
                GcodeLine gcodeline = Gcode.Where(x => x.Index == nextindexforbuffer).First();
                gcodeline.InSerialBuffer = false;
                gcodeline.IsProcessed = true;

                queuesize -= gcodeline.GrblCommand.Length;
                gcodeline.Response = _iserror ? Error.Codes[_data.Split(':')[1]] : _data;
                GcodeLineChanged?.Invoke(gcodeline, new EventArgs());

                ++nextindexforbuffer;

                await Task.Run(() => ProcessNextGcode());
            }
            else
            {
                if (nextindexforbuffer >= Gcode?.Count)
                {
                    StopProcessingGcode();
                }

                if (_iserror)
                {
                    ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs(Error.Codes[_data.Split(':')[1]]));
                }
            }

            GetStatus();
        }
        #endregion

        #region Gcode processing
        private GcodeCollection gcode;
        public GcodeCollection Gcode
        {
            get
            {
                return gcode;
            }
            set
            {
                if (gcode != value)
                {
                    gcode = value;
                    GcodeChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        public bool GcodeIsRunning { get; set; } = false;
        private int queuesize = 0;
        private int rx_buffer_size = 128;
        private int nextindexforbuffer = 0;

        public void StopProcessingGcode()
        {
            GcodeIsRunning = false;
        }

        public async Task StartProcessingGcodeAsync()
        {
            if (Gcode != null & (InIdleState | InCheckState))
            {
                rx_buffer_size = Convert.ToInt16(ConfigurationManager.AppSettings["CommandBufferCapacity"]);
                Gcode.Reset();
                GcodeIsRunning = true;
                nextindexforbuffer = 0;
                queuesize = 0;

                await Task.Run(() => ProcessNextGcode());
            }
        }

        private void ProcessNextGcode()
        {
            var takeabreak = !InIdleState & !InCheckState & !InRunState;

            GcodeLine gcodeline = Gcode.Where(x => !x.IsProcessed & !x.InSerialBuffer).FirstOrDefault();
            
            while (gcodeline!=null && !takeabreak && (GcodeIsRunning & ((rx_buffer_size - queuesize) >= gcodeline.GrblCommand.Length)))
            {
                gcodeline.InSerialBuffer = true;
                gcodeline.Response = "Buffered";
                queuesize += gcodeline.GrblCommand.Length;
                serialport.Write(Command.Gcode(gcodeline.GrblCommand).ToString());
                GcodeLineChanged?.Invoke(gcodeline, new EventArgs());

                gcodeline = Gcode.Where(x => !x.IsProcessed & !x.InSerialBuffer).FirstOrDefault();
            }
        }
    

        #endregion

        #region Feedback
        private string feedbackmessage = string.Empty;
        public string FeedbackMessage
        {
            get
            {
                return feedbackmessage;
            }
            set
            {
                if (feedbackmessage != value)
                {
                    feedbackmessage = value;
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(feedbackmessage));
                }
            }
        }

        private void ProcessFeedback(string _feedback)
        {
            FeedbackMessage = _feedback.Replace("[MSG:", "").Replace("]", "");
        }
        #endregion

        #region Statuspolling
        public bool StatusIsPolled { get; private set; } = false;
        private int pollinginterval = 500;

        public void StopStatuspolling()
        {
            StatusIsPolled = false;
        }

        public async void StartStatuspolling()
        {
            await Task.Run(() => PollStatus());
        }

        private void PollStatus()
        {
            if (StatusIsPolled) return; else StatusIsPolled = true;

            while (StatusIsPolled)
            {
                Thread.Sleep(pollinginterval);
                serialport.Write(Command.GetStatus.ToString());
            }

            return;
        }

      
        #endregion

        #region State
        public int SpindleSpeed { get; private set; }

        private int feedrate = 0;
        public int FeedRate
        {
            get
            {
                return feedrate;
            }
            set
            {
                if (feedrate != value)
                {
                    FeedRateChanged?.Invoke(this, new FeedRateChangedEventArgs(feedrate, value));
                    feedrate = value;
                }
            }
        }

        private void ProcessFeedAndSpindle(string _feedandspindle)
        {
            string[] parts = _feedandspindle.ToLower().Split(',');
            FeedRate = int.TryParse(parts[0], out int cfr) ? cfr : FeedRate;
            SpindleSpeed = int.TryParse(parts[1], out int css) ? css : SpindleSpeed;
        }

        public string StateMessage { get; private set; }

        private void ProcessState(string _state, string _alarmmessage = null)
        {
            string state = _state.Replace(":", "");
            GrblState currentstate = (GrblState)Enum.Parse(typeof(GrblState), state);
            StateMessage = State != currentstate ? _alarmmessage ?? Codes.State.Codes[currentstate] : StateMessage;
            State = currentstate;
        }

        private GrblState state = GrblState.UNKNOWN;
        public GrblState State
        {
            get
            {
                return state;
            }
            set
            {
                if (value !=state)
                {
                    state = value;
                    StateChanged?.Invoke(this, new EventArgs());

                    switch (state)
                    {
                        case GrblState.IDLE:
                            {
                                if (!Settings.Loaded) GetSettings();
                                StopStatuspolling();
                            }
                            break;
                        
                        case GrblState.HOME:
                            StateMessage = Codes.State.Codes[state];
                            break;
                        case GrblState.ALARM:
                            GcodeIsRunning = false;
                            StopStatuspolling();
                            break;
                        case GrblState.CHECK:
                        case GrblState.SLEEP:
                            StopStatuspolling();
                            break;
                        case GrblState.RUN:
                        case GrblState.JOG:
                            StartStatuspolling();
                            break;
                    }
                }
            }
        }
                
        public bool InIdleState
        {
            get
            {
                return State == GrblState.IDLE;
            }
        }
        public bool InRunState
        {
            get
            {
                return State == GrblState.RUN;
            }
        }
        public bool InHoldState
        {
            get
            {
                return  (State == GrblState.HOLD0 | State == GrblState.HOLD1);
            }
        }
        public bool InJogState
        {
            get
            {
                return State == GrblState.JOG;
            }
        }
        public bool InAlarmState
        {
            get
            {
                return State == GrblState.ALARM;
            }
        }
        public bool InDoorState
        {
            get
            {
                return (State == GrblState.DOOR0 | State == GrblState.DOOR1 | State == GrblState.DOOR2 | State == GrblState.DOOR3);
            }
        }
        public bool InCheckState
        {
            get
            {
                return State == GrblState.CHECK;
            }
        }
        public bool InHomeState
        {
            get
            {
                return State == GrblState.HOME;
            }
        }
        public bool InSleepState
        {
            get
            {
                return State == GrblState.SLEEP;
            }
        }

        #endregion

        #region Parserstate

        private CoolingState coolingstate = CoolingState.UNKNOWN;
        public CoolingState CoolingState
        {
            get
            {
                return coolingstate;
            }
            set
            {
                if (coolingstate != value)
                {
                    coolingstate = value;
                    CoolingStateChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        private SpindleState spindlestate = SpindleState.UNKNOWN;
        public SpindleState SpindleState
        {
            get
            {
                return spindlestate;
            }
            set
            {
                if (spindlestate != value)
                {
                    spindlestate = value;
                    SpindleStateChanged?.Invoke(this, new EventArgs());

                    FeedbackMessage = spindlestate == SpindleState.ON ? "Spindle started" : spindlestate == SpindleState.OFF ? "Spindle stopped" : "";
                }
            }
        }

        private List<string> parserstate = new List<string>();
        public List<string> ParserState
        {
            get
            {
                return parserstate;
            }
            set
            {
                if (parserstate != value)
                {
                    parserstate = value;
                }
            }
        }

        private void ProcessGcodeParameter(string _gcodeparameter)
        {
            string[] parts = _gcodeparameter.Replace("[", "").Replace("]", "").Split(':');
            string[] coords = parts[1].Split(',');

            WorkCoordinates[parts[0]].X = Convert.ToDouble(coords[0]);
            WorkCoordinates[parts[0]].Y = Convert.ToDouble(coords[1]);
            WorkCoordinates[parts[0]].Z = Convert.ToDouble(coords[2]);

            if (WorkCoordinates[parts[0]].IsCurrent) CurrentWorkCoordinateChanged?.Invoke(this, new EventArgs());
        }

        private void ProcessParserState(string _parserstate)
        {
            ParserState = _parserstate.Replace("[GC:", "").Replace("]", "").Split(' ').ToList();

            foreach (var workcoordinate in WorkCoordinates.Values)
            {
                if (
                    (ParserState.Contains(workcoordinate.Name) & !workcoordinate.IsCurrent)
                    ||
                    (workcoordinate.IsCurrent & !ParserState.Contains(workcoordinate.Name))
                   )
                {
                    workcoordinate.IsCurrent = ParserState.Contains(workcoordinate.Name);
                    if (workcoordinate.IsCurrent) CurrentWorkCoordinateChanged?.Invoke(this, new EventArgs());
                }
            }

            SpindleState = ParserState.Contains("M3") ? SpindleState.ON : (ParserState.Contains("M5") ? SpindleState.OFF : SpindleState.UNKNOWN);
            CoolingState = ParserState.Contains("M7") ? CoolingState.MIST_ON : (ParserState.Contains("M8") ? CoolingState.FLOOD_ON : (ParserState.Contains("M9") ? CoolingState.OFF : CoolingState.UNKNOWN));
        }
        #endregion

        #region Settings

        public bool HardlimitsEnabled
        {
            get
            {
                return Convert.ToBoolean(Settings["$21"].Content);
            }
        }

        public bool SoftlimitsEnabled
        {
            get
            {
                return Convert.ToBoolean(Settings["$20"].Content);
            }
        }

        public bool HomingCycleEnabled
        {
            get
            {
                return Convert.ToBoolean(Settings["$22"].Content);
            }
        }

        public Settings Settings { get; private set; } = Settings.List;

        private void ProcessSetting(string _data)
        {
            string[] parts = _data.Split('=');
            Settings[parts[0]].Content = parts[1];
        }

        #endregion

        #region Commands

        public void StartSpindle()
        {
            if (InIdleState & SpindleState != SpindleState.ON)
            {
                serialport.Write(Command.StartSpindle.ToString());
                GetParserstate();
            }
        }

        public void StopSpindle()
        {
            if (InIdleState & SpindleState == SpindleState.ON)
            {
                serialport.Write(Command.StopSpindle.ToString());
                GetParserstate();
            }
        }

        public void GetStatus()
        {
            if (!InCheckState & !StatusIsPolled) serialport.Write(Command.GetStatus.ToString());
        }

        public void Reset()
        {
            GcodeIsRunning = false;
            FeedbackMessage = null;
            State = GrblState.UNKNOWN;
            IsReset?.Invoke(this, new EventArgs());
            serialport.Write(Command.Reset.ToString());
        }

        public void KillAlarmLock()
        {
            if (InAlarmState)
            {
                serialport.Write(Command.KillAlarmLock.ToString());
            }
        }

        public void GetSettings()
        {
           if (InIdleState) serialport.Write(Command.GetSettings.ToString());
        }

        public void SendCommand(string _command)
        {
            serialport.Write(Command.Gcode(_command).ToString());

           // Debug.WriteLine(_command);
        }

        public void IncreaseFeed1()
        {
            if (InRunState) serialport.Write(Command.IncreaseFeed1.ToString());
        }
        public void IncreaseFeed10()
        {
            if (InRunState) serialport.Write(Command.IncreaseFeed10.ToString());
        }

        public void DecreaseFeed1()
        {
            if (InRunState) serialport.Write(Command.DecreaseFeed1.ToString());
        }

        public void DecreaseFeed10()
        {
            if (InRunState) serialport.Write(Command.DecreaseFeed10.ToString());
        }

        public void SetFeed100()
        {
            if (InRunState) serialport.Write(Command.SetFeed100.ToString());
        }

        public void SetRapid100()
        {
            if (InRunState) serialport.Write(Command.SetRapid100.ToString());
        }

        public void SetRapid50()
        {
            if (InRunState) serialport.Write(Command.SetRapid50.ToString());
        }

        public void SetRapid25()
        {
            if (InRunState) serialport.Write(Command.SetRapid25.ToString());
        }

        public async void StartCycleAsync()
        {
            if (InHoldState | InDoorState) serialport.Write(Command.StartCycle.ToString());

            if (GcodeIsRunning) await Task.Run(() => ProcessNextGcode());
        }

        public void HoldFeed()
        {
            if (InRunState) serialport.Write(Command.HoldFeed.ToString());
        }

        public void GetHelp()
        {
            if (InIdleState) serialport.Write(Command.GetHelp.ToString());
        }

        public void GetParserstate()
        {
            serialport.Write(Command.GetParserstate.ToString());
        }

        public void GetBuildInfo()
        {
            if (InIdleState) serialport.Write(Command.GetBuildInfo.ToString());
        }

        public void GetGcodeParameters()
        {
            serialport.Write(Command.GetGcodeParameters.ToString());
        }

        public void SafetyDoor()
        {
            serialport.Write(Command.SafetyDoor.ToString());
        }

        public void GetStartupBlocks()
        {
            if (InIdleState) serialport.Write(Command.GetStartupBlocks.ToString());
        }

        public void EnableCheckmode()
        {
            if (InIdleState) serialport.Write(Command.ToggleCheckmode.ToString());
        }

        public void DisableCheckmode()
        {
            if (InCheckState) serialport.Write(Command.ToggleCheckmode.ToString());
        }

        public void RunHomingcycle()
        {
            if (InIdleState)
            {
                serialport.Write(Command.RunHomingcycle.ToString());
                State = GrblState.HOME;
            }
        }

        #endregion

        #region Jogging

        public void JogLeft(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91X-", MaxTravelDistanceLeftDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogRight(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91X", MaxTravelDistanceRightDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogForward(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91Y", MaxTravelDistanceForwardDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogBack(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91Y-", MaxTravelDistanceBackDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogUp(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91Z", MaxTravelDistanceUpDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogDown(string _feed = null)
        {
            if (InIdleState)
            {
                _feed = _feed ?? "F2000";
                string _command = string.Concat(_feed, "G21G91Z-", MaxTravelDistanceDownDirection);
                serialport.Write(Command.Jog(_command).ToString());
            }
        }

        public void JogCancel()
        {
            serialport.Write(Command.JogCancel.ToString());
        }

        #endregion

        #region Everything about position, distances and speeds

        public WorkCoordinates WorkCoordinates { get; private set; } = WorkCoordinates.List;

        #region Work Coordinate Offset

        //Sum of the current work coordinate system, G92 offsets, and G43.1 tool length offset.
        public Coordinate WCO { get; private set; } = new Coordinate() { Type = "WCO" };
        private void WCO_CoordinateChanged(object sender, EventArgs e)
        {
            CoordinateChanged?.Invoke(sender, e);
        }

        private void ProcessWCO(string _wco)
        {
            string[] axes = _wco.ToLower().Split(',');
            double x_temp = double.TryParse(axes[0], out x_temp) ? x_temp : WCO.X;
            double y_temp = double.TryParse(axes[1], out y_temp) ? y_temp : WCO.Y;
            double z_temp = double.TryParse(axes[2], out z_temp) ? z_temp : WCO.Z;

            WCO.Update(x_temp, y_temp, z_temp);
            MPOS.Update(WPOS.X + WCO.X, WPOS.Y + WCO.Y, WPOS.Z + WCO.Z);
            WPOS.Update(MPOS.X - WCO.X, MPOS.Y - WCO.Y, MPOS.Z - WCO.Z);
        }
        #endregion

        #region Machine position
        public Coordinate MPOS { get; private set; } = new Coordinate() { Type = "MPOS" };
        private void MPOS_CoordinateChanged(object sender, EventArgs e)
        {
            CoordinateChanged?.Invoke(sender, e);
        }

        private void ProcessMPOS(string _mpos)
        {
            string[] axes = _mpos.ToLower().Split(',');
            double x_temp = double.TryParse(axes[0], out x_temp) ? x_temp : MPOS.X;
            double y_temp = double.TryParse(axes[1], out y_temp) ? y_temp : MPOS.Y;
            double z_temp = double.TryParse(axes[2], out z_temp) ? z_temp : MPOS.Z;

            MPOS.Update(x_temp, y_temp, z_temp);
            WPOS.Update(MPOS.X - WCO.X, MPOS.Y - WCO.Y, MPOS.Z - WCO.Z);
        }
        #endregion

        #region Workposition
        public Coordinate WPOS { get; private set; } = new Coordinate() { Type = "WPOS" };
        private void WPOS_CoordinateChanged(object sender, EventArgs e)
        {
            CoordinateChanged?.Invoke(sender, e);
        }

        private void ProcessWPOS(string _mpos)
        {
            string[] axes = _mpos.ToLower().Split(',');

            double x_temp = double.TryParse(axes[0], out x_temp) ? x_temp : WPOS.X;
            double y_temp = double.TryParse(axes[1], out y_temp) ? y_temp : WPOS.Y;
            double z_temp = double.TryParse(axes[2], out z_temp) ? z_temp : WPOS.Z;

            WPOS.Update(x_temp, y_temp, z_temp);
            MPOS.Update(WPOS.X + WCO.X, WPOS.Y + WCO.Y, WPOS.Z + WCO.Z);
        }

        public void SetXWorkToZero()
        {
            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} X{1}\r", WorkCoordinates.Current.Index, MPOS.X)).ToString());
            GetGcodeParameters();
        }

        public void SetCorrectedXWorkToZero()
        {
            double correctionx= Convert.ToDouble(ConfigurationManager.AppSettings["CorrectionX"]);

            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} X{1}\r", WorkCoordinates.Current.Index, MPOS.X+ correctionx)).ToString());
            GetGcodeParameters();
        }

        public void SetYWorkToZero()
        {
            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} Y{1}\r", WorkCoordinates.Current.Index, MPOS.Y)).ToString());
            GetGcodeParameters();
        }

        public void SetCorrectedYWorkToZero()
        {
            double correctiony = Convert.ToDouble(ConfigurationManager.AppSettings["CorrectionY"]);
            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} Y{1}\r", WorkCoordinates.Current.Index, MPOS.Y+ correctiony)).ToString());
            GetGcodeParameters();
        }

        public void SetZWorkToZero()
        {
            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} Z{1}\r", WorkCoordinates.Current.Index, MPOS.Z)).ToString());
            GetGcodeParameters();
        }

        public void SetCorrectedZWorkToZero()
        {
            double correctionz = Convert.ToDouble(ConfigurationManager.AppSettings["CorrectionZ"]);
            serialport.Write(Command.Gcode(string.Format("G10 L2 P{0} Z{1}\r", WorkCoordinates.Current.Index, MPOS.Z+ correctionz)).ToString());
            GetGcodeParameters();
        }

        #endregion

        public void GoToWorkZero()
        {
            if (InIdleState) serialport.Write(Command.Gcode("G90 G0 X0 Y0 Z0").ToString());
        }

        

        #region Distance
        public double HomingPullOff
        {
            get
            {
                return Convert.ToDouble(Settings["$27"].Content); 
            }
        }

        public double MaxXDistance
        {
            get
            {
                return Convert.ToDouble(Settings["$130"].Content);
            }
        }

        public double MaxYDistance
        {
            get
            {
                return Convert.ToDouble(Settings["$131"].Content);
            }
        }

        public double MaxZDistance
        {
            get
            {
                return Convert.ToDouble(Settings["$132"].Content);
            }
        }

        /*
            Ok, we want the max travel distance in 6 possible directions (Left, Right, Back Forward, Up and Down), but there are 4 possibilities
            0)  No hardlimits, No Softlimits => that's dangerous because the machine could easily crash into the limits
            1)  No softlimits, Hardlimits enabled. =>nothing to worry about. The machine halts itself on hitting the sensors
            2)  No hardlimits, but softlimits enabled. => now it's important to calculate the maxtraveldistance from the current machineposition 
                  and the max traveldistances left on that axe in the requested direction
            3)  Hardlimits enabled and softlimits enabled => Same situation as in 2). We need to calculate the maxtravel dist for each axe
                but.... 0,0,0 is likely not to be reached because of the hardlimits (if hardlimits are enabled). 
                in my case because Z0 is most upperlimit and exact where the hardlimitsensor sits. it's an optical light sensor so 
                there's some inaccuracy in it. Going Z0 without triggering the hardlimit is not possible
                X and Y are allways in negative space and for my machineconfiguration X0 and Y0 is the upper right corner 
                stepper XYZ -direction is therefore inverted. Homedir mask is 01100000 (96) wich means XY lowerleft corner is XY-plane home
                And, as i said, topmost of the Z is Z0.
        */

        public double MaxTravelDistanceLeftDirection
        {
            get
            {
                return SoftlimitsEnabled ? (MaxXDistance + MPOS.X - HomingPullOff) : HardlimitsEnabled ? MaxXDistance : 50;
            }
        }

        public double MaxTravelDistanceRightDirection
        {
            get
            {
                return SoftlimitsEnabled ? (-1 * MPOS.X) - HomingPullOff : HardlimitsEnabled ? MaxXDistance : 50;
            }
        }

        public double MaxTravelDistanceForwardDirection
        {
            get
            {
                return SoftlimitsEnabled ? (-1 * MPOS.Y) - HomingPullOff : HardlimitsEnabled ? MaxYDistance : 50;
            }
        }

        public double MaxTravelDistanceBackDirection
        {
            get
            {
                return SoftlimitsEnabled ? (MaxYDistance + MPOS.Y - HomingPullOff) : HardlimitsEnabled ? MaxYDistance : 50;
            }
        }
        //Be careful! Toollength is not in the calculation
        public double MaxTravelDistanceDownDirection
        {
            get
            {
                return SoftlimitsEnabled ? (MaxZDistance + MPOS.Z - HomingPullOff) : HardlimitsEnabled ? MaxZDistance : 50;
            }
        }

        public double MaxTravelDistanceUpDirection
        {
            get
            {
                return SoftlimitsEnabled ? (-1 * MPOS.Z) - HomingPullOff : HardlimitsEnabled ? MaxZDistance : 50;
            }
        }
        #endregion

        #region Rate
        public double MaxXRate
        {
            get
            {
                return Convert.ToDouble(Settings["$110"].Content);
            }
        }

        public double MaxYRate
        {
            get
            {
                return Convert.ToDouble(Settings["$111"].Content);
            }
        }

        public double MaxZRate
        {
            get
            {
                return Convert.ToDouble(Settings["$112"].Content);
            }
        }

        public double MaxRate
        {
            get
            {
                return Settings.Count == 0 ? 0 : MaxXRate > MaxYRate ? MaxXRate > MaxZRate ? MaxXRate : MaxZRate : MaxYRate > MaxZRate ? MaxYRate : MaxZRate;
            }
        }
        #endregion

        #region Acceleration
        public double MaxXAcceleration
        {
            get
            {
                return Convert.ToDouble(Settings["$120"].Content);
            }
        }

        public double MaxYAcceleration
        {
            get
            {
                return Convert.ToDouble(Settings["$121"].Content);
            }
        }

        public double MaxZAcceleration
        {
            get
            {
                return Convert.ToDouble(Settings["$122"].Content);
            }
        }
        #endregion

        #endregion
    }
}
