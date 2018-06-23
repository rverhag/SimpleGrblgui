using System.ComponentModel;

namespace Vhr.Gcode
{
    public sealed class GcodeLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsProcessed { get; set; }
        public bool InSerialBuffer { get; set; }
        public int SerialBufferLength { get; set; }

        public string response;
        public string Response
        {
            get { return response; }
            set
            {
                if (value != response)
                {
                    response = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Response"));
                }
            }
        }
        public int Index { get; set; }
        public string Raw { get; set; }
        public string GrblCommand { get; set; }

        public string LineNumber { get; set; }
        public Plane Plane { get; set; }
        public bool Isabsolutedistance { get; set; }
        public bool IsMetric { get; set; }
        public bool IsCuttingMotion { get; set; }
        public bool IsLinearMotion { get; set; }
        public bool? IsCW { get; set; }

        public double Xfrom { get; set; }
        public double Yfrom { get; set; }
        public double Zfrom { get; set; }

        public double Xto { get; set; }
        public double Yto { get; set; }
        public double Zto { get; set; }

        public double Xcenter { get; set; }
        public double Ycenter { get; set; }
        public double Zcenter { get; set; }

        public double I { get; set; }
        public double J { get; set; }
        public double K { get; set; }
        public double P { get; set; }
        public double R { get; set; }

        public override string ToString()
        {
            return Raw;
        }
    }
}
