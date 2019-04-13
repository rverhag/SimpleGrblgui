using System.Collections.Generic;

namespace Vhr.Codes
{
    public sealed class Gcode:  Dictionary<string, Gcode>
    {
        //set by constructor
        public readonly string Modalgroup;
        public readonly string Description;

        //constructor
        public Gcode(string _modalgroup,  string _description)
        {
            Modalgroup = _modalgroup;
            Description = _description;
        }

        public Gcode()
        {
            Add("G0", new Gcode("1G",  "Rapid Move"));
            Add("G00", new Gcode("1G",  "Rapid Move"));

            Add("G1", new Gcode("1G",  "Linear Move"));
            Add("G01", new Gcode("1G",  "Linear Move"));

            Add("G2", new Gcode("1G",  "Arc Move (clockwise)"));
            Add("G02", new Gcode("1G",  "Arc Move (clockwise)"));

            Add("G3", new Gcode("1G",  "Arc Move (counterclockwise)"));
            Add("G03", new Gcode("1G",  "Arc Move (counterclockwise)"));

            Add("G4", new Gcode("1G",  "Dwell"));
            Add("G04", new Gcode("1G",  "Dwell"));

            Add("G5", new Gcode("1G",  "Cubic Spline"));
            Add("G05", new Gcode("1G", "Cubic Spline"));

            Add("G38.2", new Gcode("1G", "Probe toward workpiece, stop on contact, signal error if failure"));
            Add("G38.3", new Gcode("1G", "Probe toward workpiece, stop on contact"));
            Add("G38.4", new Gcode("1G", "Probe away from workpiece, stop on loss of contact, signal error if failure"));
            Add("G38.5", new Gcode("1G", "Probe away from workpiece, stop on loss of contact"));



            Add("G53", new Gcode("0G", "Move in Machine Coordinates."));

            Add("G54", new Gcode("12G", "Select coordinate system 1."));
            Add("G55", new Gcode("12G", "Select coordinate system 2"));
            Add("G56", new Gcode("12G", "Select coordinate system 3"));
            Add("G57", new Gcode("12G", "Select coordinate system 4"));
            Add("G58", new Gcode("12G", "Select coordinate system 5"));
            Add("G59", new Gcode("12G", "Select coordinate system 6"));
            

            Add("G80", new Gcode("1G", "Cancel canned cycle modal motion."));

            Add("G90", new Gcode("3G", "Absolute distance mode."));
            Add("G91", new Gcode("3G", "Incremental distance mode."));

            Add("G92", new Gcode("0G", "Coordinate System Offset."));
            Add("G92.1", new Gcode("0G", "Turn off G92 offsets and reset parameters."));

            Add("G93", new Gcode("5G", "Inverse Time Mode."));
            Add("G94", new Gcode("5G", "Units per Minute Mode."));


            Add("M0", new Gcode("4M", "Pause the running program temporarily"));
            Add("M2", new Gcode("4M", "End the program."));
            Add("M30", new Gcode("4M", "Exchange pallet shuttles and end the program"));

            Add("M3", new Gcode("7M", "Start the spindle clockwise at the S speed."));
            Add("M4", new Gcode("7M", "Start the spindle counterclockwise at the S speed."));
            Add("M5", new Gcode("7M", "Stop the spindle."));

            Add("M7", new Gcode("8M", "Turn mist coolant on."));
            Add("M8", new Gcode("8M", "Turn coolant on."));
            Add("M9", new Gcode("8M", "Turn coolant off."));

        }
        
        public static readonly Gcode Codes = new Gcode();
    }
}
