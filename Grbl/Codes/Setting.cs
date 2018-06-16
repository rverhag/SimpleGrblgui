using System.Collections.Generic;

namespace Vhr.Codes
{
    public class Setting : Dictionary<string, string>
    {
        public Setting()
        {
            Add("$0", "Step pulse time, microseconds");
            Add("$1", "Step idle delay, milliseconds");
            Add("$2", "Step pulse invert, mask");
            Add("$3", "Step direction invert, mask");
            Add("$4", "Invert step enable pin, boolean");
            Add("$5", "Invert limit pins, boolean");
            Add("$6", "Invert probe pin, boolean");

            Add("$10", "Status report options, mask");
            Add("$11", "Junction deviation, millimeters");
            Add("$12", "Arc tolerance, millimeters");
            Add("$13", "Report in inches, boolean");

            Add("$20", "Soft limits enable, boolean");
            Add("$21", "Hard limits enable, boolean");
            Add("$22", "Homing cycle enable, boolean");
            Add("$23", "Homing direction invert, mask");
            Add("$24", "Homing locate feed rate, mm/min");
            Add("$25", "Homing search seek rate, mm/min");
            Add("$26", "Homing switch debounce delay, milliseconds");
            Add("$27", "Homing switch pull-off distance, millimeters");

            Add("$30", "Maximum spindle speed, RPM");
            Add("$31", "Minimum spindle speed, RPM");
            Add("$32", "Laser-mode enable, boolean");

            Add("$100", "X-axis steps per millimeter");
            Add("$101", "Y-axis steps per millimeter");
            Add("$102", "Z-axis steps per millimeter");

            Add("$110", "X-axis maximum rate, mm/min");
            Add("$111", "Y-axis maximum rate, mm/min");
            Add("$112", "Z-axis maximum rate, mm/min");

            Add("$120", "X-axis acceleration, mm/sec^2");
            Add("$121", "Y-axis acceleration, mm/sec^2");
            Add("$122", "Z-axis acceleration, mm/sec^2");

            Add("$130", "X-axis maximum travel, millimeters");
            Add("$131", "Y-axis maximum travel, millimeters");
            Add("$132", "Z-axis maximum travel, millimeters");
        }

        public static readonly Setting Codes = new Setting();
    }
}
