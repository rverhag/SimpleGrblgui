namespace Vhr.Types
{
    public sealed class Command 
    {
        //set by constructor
        public readonly string GrblCommand;
        public readonly string GrblString;
        public readonly string Description;

        //constructor
        public Command(string _grblcommand, string _description, string _grblstring = null)
        {
            GrblCommand = _grblcommand;
            Description = _description;
            GrblString = _grblstring;
        }

        public override string ToString()
        {
            return GrblCommand;
        }

        public static readonly Command Reset = new Command("\x18", "Soft-Reset", "ctrl-x");
        public static readonly Command GetStatus = new Command("\x3f", "Status report query", "?");
        public static readonly Command GetSettings = new Command("\x24\x24\r", "Get Settings", "$$");
        public static readonly Command KillAlarmLock = new Command("\x24\x58\r", "Kill Alarm Lock", "$X");
        public static readonly Command StartCycle = new Command("\x7e", "Cycle start/resume", "~");
        public static readonly Command HoldFeed = new Command("\x21", "Feed Hold", "!");
        public static readonly Command Enter = new Command("\r", "Enter", "enter");
        public static readonly Command GetHelp = new Command("\x24\r", "Get help", "$");
        public static readonly Command GetGcodeParameters = new Command("\x24\x23\r", "Get Gcode Parameters", "/$#");
        public static readonly Command GetParserstate = new Command("\x24\x47\r", "Get gcode Parserstate", "$G");
        public static readonly Command GetBuildInfo = new Command("\x24\x49\r", "Get Build Info", "$I");
        public static readonly Command GetStartupBlocks = new Command("\x24\x4e\r", "Get Startup Blocks", "$N");
        public static readonly Command ToggleCheckmode = new Command("\x24\x43\r", "Toggle Checkmode", "$C");
        public static readonly Command RunHomingcycle = new Command("\x24\x48\r", "Run Homingcycle", "$H");
        public static readonly Command SafetyDoor = new Command("\x84", "Safety door");
        public static readonly Command SetFeed100 = new Command("\x90", "Set feed 100%");
        public static readonly Command IncreaseFeed10 = new Command("\x91", "Increase feed 10%");
        public static readonly Command DecreaseFeed10 = new Command("\x92", "Decrease feed 10%");
        public static readonly Command IncreaseFeed1 = new Command("\x93", "Increase feed 1%");
        public static readonly Command DecreaseFeed1 = new Command("\x94", "Decrease feed 1%");
        public static readonly Command SetRapid100 = new Command("\x95", "Set rapid 100%");
        public static readonly Command SetRapid50 = new Command("\x96", "Set rapid 50%");
        public static readonly Command SetRapid25 = new Command("\x97", "Set rapid 25%");
        public static readonly Command SetSpindle100 = new Command("\x95", "Set spindle 100%");
        public static readonly Command IncreaseSpindle10 = new Command("\x9A", "Increase spindle 10%");
        public static readonly Command DecreaseSpindle10 = new Command("\x9B", "Decrease spindle 10%");
        public static readonly Command IncreaseSpindle1 = new Command("\x9C", "Increase spindle 1%");
        public static readonly Command DecreaseSpindle1 = new Command("\x9D", "Decrease spindle 1%");
        public static readonly Command StartSpindle = new Command("M3\r", "Start Spindle", "M3");
        public static readonly Command StopSpindle = new Command("M5\r", "Stop Spindle", "M5");
        public static readonly Command ToggleSpindleStop = new Command("\x9E", "ToggleSpindleStop");
        public static readonly Command StartCooling = new Command("M8\r", "Start Cooling", "M8");
        public static readonly Command StopCooling = new Command("M9\r", "Stop Cooling", "M9");
        public static readonly Command ToggleFloodCoolant = new Command("\xA0", "Toggle FloodCoolant");
        public static readonly Command ToggleMistCoolant = new Command("\xA1", "Toggle MistCoolant");
        public static readonly Command JogCancel = new Command("\x85", "Jog cancel");
       

        public static Command Jog(string _command)
        {
            return new Command(string.Concat("$J=", _command, "\r"), string.Concat("$J=", _command, "\r"), "Jog");
        }

        public static Command Gcode(string _command)
        {
            return new Command(string.Concat(_command, "\r"), _command, "gcode");
        }
    }
}
