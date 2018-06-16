using System;
using System.Collections.Generic;
using System.Linq;

namespace Vhr.Types
{
    public sealed class WorkCoordinates : Dictionary<string, WorkCoordinate>
    {
        private static readonly Lazy<WorkCoordinates> workcoordinates = new Lazy<WorkCoordinates>(() => new WorkCoordinates());
        public static WorkCoordinates List
        {
            get { return workcoordinates.Value; }
        }

        private WorkCoordinates()
        {
            Add("G54", new WorkCoordinate("G54", 1) { IsCurrent = true });
            Add("G55", new WorkCoordinate("G55", 2));
            Add("G56", new WorkCoordinate("G56", 3));
            Add("G57", new WorkCoordinate("G57", 4));
            Add("G58", new WorkCoordinate("G58", 5));
            Add("G59", new WorkCoordinate("G59", 6));
        }

        public WorkCoordinate Current { get { return this.Where(x => x.Value.IsCurrent).First().Value; } }
    }

    public sealed class WorkCoordinate
    {
        public readonly string Name;
        public readonly int Index;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool IsCurrent { get; set; } = false;

        public WorkCoordinate(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
