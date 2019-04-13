using System.Collections.Generic;

namespace Vhr.Codes
{
    public sealed class Alarm : Dictionary<string, string>
    {
        public Alarm()
        {
            Add("1", "Hard limit triggered. Machine position is likely lost due to sudden and immediate halt. Re-homing is highly recommended.");
            Add("2", "Soft limit triggered. G-code motion target exceeds machine travel. Machine position safely retained. Alarm may be unlocked.");
            Add("3", "Reset while in motion. Grbl cannot guarantee position. Lost steps are likely. Re-homing is highly recommended.");
            Add("4", "Probe fail. The probe is not in the expected initial state before starting probe cycle, where G38.2 and G38.3 is not triggered and G38.4 and G38.5 is triggered.");
            Add("5", "Probe fail. Probe did not contact the workpiece within the programmed travel for G38.2 and G38.4.");
            Add("6", "Homing fail. Reset during active homing cycle.");
            Add("7", "Homing fail. Safety door was opened during active homing cycle.");
            Add("8", "Homing fail. Cycle failed to clear limit switch when pulling off. Try increasing pull-off setting or check wiring.");
            Add("9", "Homing fail. Could not find limit switch within search distance. Defined as 1.5 * max_travel on search and 5 * pulloff on locate phases.");
        }

        public static readonly Alarm Codes = new Alarm();
    }
}
