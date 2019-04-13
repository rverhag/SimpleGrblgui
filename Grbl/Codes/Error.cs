using System.Collections.Generic;

namespace Vhr.Codes
{
    public sealed class Error : Dictionary<string, string>
    {
        public Error()
        {
            Add("1", "G-code words consist of a letter and a value. Letter was not found.");
            Add("2", "Numeric value format is not valid or missing an expected value.");
            Add("3", "Grbl '$' system command was not recognized or supported.");
            Add("4", "Negative value received for an expected positive value.");
            Add("5", "Homing cycle is not enabled via settings.");
            Add("6", "Minimum step pulse time must be greater than 3 usec");
            Add("7", "EEPROM read failed. Reset and restored to default values.");
            Add("8", "Grbl '$' command cannot be used unless Grbl is IDLE. Ensures smooth operation during a job.");
            Add("9", "G-code locked out during alarm or jog state.");
            Add("10", "Soft limits cannot be enabled without homing also enabled.");
            Add("11", "Max characters per line exceeded. Line was not processed and executed.");
            Add("12", "(Compile Option) Grbl '$' setting value exceeds the maximum step rate supported.");
            Add("13", "Safety door detected as opened and door state initiated.");
            Add("14", "(Grbl-Mega Only) Build info or startup line exceeded EEPROM line length limit.");
            Add("15", "Jog target exceeds machine travel. Command ignored.");
            Add("16", "Jog command with no '=' or contains prohibited g-code.");
            Add("17", "Laser mode requires PWM output.");
            Add("20", "Unsupported or invalid g-code command found in block.");
            Add("21", "More than one g-code command from same modal group found in block.");
            Add("22", "Feed rate has not yet been set or is undefined.");
            Add("23", "A G or M command value in the block is not an integer. For example,G4 can't be G4.13. Some g-code commands are floating point (G92.1), but these are ignored.");
            Add("24", "Two g-code commands that both require the use of the XYZ axis words were detected in the block.");
            Add("25", "A g-code word was repeated in the block.");
            Add("26", "A g-code command implicitly or explicitly requires XYZ axis words in the block, but none were detected.");
            Add("27", "The g-code protocol mandates N line numbers to be within the range of 1-99,999. We think that's a bit silly and arbitrary. So, we increased the max number to 9,999,999. This error occurs when you send a number more than this.");
            Add("28", "A g-code command was sent, but is missing some important P or Lvalue words in the line. Without them, the command can't be executed. Check your g-code.");
            Add("29", "Grbl supports six work coordinate systems G54-G59. This error happens when trying to use or configure an unsupported work coordinate system, such as G59.1, G59.2, and G59.3.");
            Add("30", "The G53 g-code command requires either a G0 seek or G1 feed motion mode to be active. A different motion was active.");
            Add("31", "There are unused axis words in the block and G80 motion mode cancel is active.");
            Add("32", "A G2 or G3 arc was commanded but there are no XYZ axis words in the selected plane to trace the arc.");
            Add("33", "The motion command has an invalid target. G2, G3, and G38.2generates this error. For both probing and arcs traced with the radius definition, the current position cannot be the same as the target. This also errors when the arc is mathematically impossible to trace, where the current position, the target position, and the radius of the arc doesn't define a valid arc.");
            Add("34", "A G2 or G3 arc, traced with the radius definition, had a mathematical error when computing the arc geometry. Try either breaking up the arc into semi-circles or quadrants, or redefine them with the arc offset definition.");
            Add("35", "A G2 or G3 arc, traced with the offset definition, is missing the IJKoffset word in the selected plane to trace the arc.");
            Add("36", "There are unused, leftover g-code words that aren't used by any command in the block.");
            Add("37", "The G43.1 dynamic tool length offset command cannot apply an offset to an axis other than its configured axis. The Grbl default axis is the Z-axis.");
            Add("38", "Tool number greater than max supported value.");
        }

        public static readonly Error Codes = new Error();
    }
}
