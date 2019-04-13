using System.Collections.Generic;
using Vhr.Enums;

namespace Vhr.Codes
{
    public sealed class State : Dictionary<GrblState, string>
    {
        public State()
        {
            Add(GrblState.IDLE, "Ready for next action.");
            Add(GrblState.RUN, "Machine is moving.");
            Add(GrblState.HOLD0, "Hold complete. Ready to resume.");
            Add(GrblState.HOLD1, "Hold in-progress. Reset will throw an alarm.");
            Add(GrblState.JOG, "Jogging, Please be careful!");
            Add(GrblState.DOOR0, "Door closed. Ready to resume.");
            Add(GrblState.DOOR1, "Machine stopped. Door still ajar. Can't resume until closed.");
            Add(GrblState.DOOR2, "Door opened. Hold (or parking retract) in-progress. Reset will throw an alarm.");
            Add(GrblState.DOOR3, "Door closed and resuming. Restoring from park, if applicable. Reset will throw an alarm.");
            Add(GrblState.CHECK, "Check - mode.");
            Add(GrblState.HOME, "Searching home.");
            Add(GrblState.SLEEP, "Sleeping.");
            Add(GrblState.ALARM, "ALARM");
            Add(GrblState.UNKNOWN, "Unknown state. Please, try getting a status from Grbl");
        }

        public static readonly State Codes = new State();
    }
}
