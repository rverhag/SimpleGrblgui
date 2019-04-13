using System.Collections.Generic;

namespace Vhr.Codes
{

    public sealed class Modalgroup : Dictionary<string, string>
    {
        public Modalgroup()
        {
            Add("0G", "Non-modal gcodes");
            Add("1G", "Motion");
            Add("2G", "Plane selection");
            Add("3G", "Distance mode");
            Add("5G", "Feed rate mode");
            Add("6G", "Units");
            Add("7G", "Cutter radius compensation");
            Add("8G", "Tool length offset");
            Add("10G", "Return mode in canned cycles");
            Add("12G", "Coordinate system selection");
            Add("13G", "path control mode");

            Add("4M", "Stopping");
            Add("6M", "Tool change");
            Add("7M", "Spindle turning");
            Add("8M", "Coolant");
            Add("9M", "Enable/diable feed an speed override switches");
        }

        public static readonly Modalgroup Codes = new Modalgroup();
    }
}
