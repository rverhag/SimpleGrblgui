using System;

namespace Vhr.Types
{
    public class Coordinate
    {
        public event EventHandler CoordinateChanged;

        public void Update(double _x, double _y, double _z)
        {
            X = _x;
            Y = _y;
            Z = _z;

            if (sumxyz != X + Y + Z) CoordinateChanged?.Invoke(this, new EventArgs());
            sumxyz = X + Y + Z;
        }

        private double sumxyz = 0;
        public string Type { get; set; }
        public double X { get; private set; } = 0;
        public double Y { get; private set; } = 0;
        public double Z { get; private set; } = 0;
    }
}
