using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vhr.Gcode
{
    public class GcodeCollection : List<GcodeLine>
    {
        #region Regex
        private Regex commentregex = new Regex(@"([^()]*)");
        private Regex coderegex = new Regex("[ngxyzfmijkpfst][+-]?[0-9]*\\.?[0-9]*", RegexOptions.IgnoreCase);
        #endregion

        public GcodeCollection(string _filename) : base()
        {
            AddRange(File.ReadAllLines(_filename).ToList().Select(
                  (line, index) => new GcodeLine()
                  {
                      Raw = line,
                      Index = index,
                  }
                  ).ToList());

            foreach (var gcodeline in this)
            {
                ParseGcode(gcodeline);
            }
        }

        public void Reset()
        {
            foreach (GcodeLine gcodeline in this)
            {
                gcodeline.Response = String.Empty;
                gcodeline.InSerialBuffer = false;
                gcodeline.IsProcessed = false;
            }
        }

        #region Gcodeparsing and pathtrace

        public double MaxX { get; private set; }
        public double MinX { get; private set; }
        public double DeltaX { get { return MaxX - MinX; } }

        public double MaxY { get; private set; }
        public double MinY { get; private set; }
        public double DeltaY { get { return MaxY - MinY; } }

        public double MaxZ { get; private set; }
        public double MinZ { get; private set; }
        public double DeltaZ { get { return MaxZ - MinZ; } }

        private Plane plane = Plane.XY;
        private bool isabsolutedistance = true;

        private bool ismetric = true;
        private bool iscoordinatedmotion = true;
        private bool islinearmotion = true;
        private bool? iscw = null;

        private double xfrom = double.NaN;
        private double yfrom = double.NaN;
        private double zfrom = double.NaN;

        private double xto = double.NaN;
        private double yto = double.NaN;
        private double zto = double.NaN;

        private double i = double.NaN;
        private double j = double.NaN;
        private double k = double.NaN;
        private double p = double.NaN;
        private double r = double.NaN;

        private void ParseGcode(GcodeLine _gcodeline)
        {
            List<string> gcodechunks = coderegex.Matches(_gcodeline.Raw.ToString()).Cast<Match>().Select(x => x.Value).ToList();

            _gcodeline.IsCuttingMotion = iscoordinatedmotion = gcodechunks.Where(x => (x.Equals("G00") | x.Equals("G0"))).FirstOrDefault() != null ? false : gcodechunks.Where(x => (x.Equals("G01") | x.Equals("G02") | x.Equals("G03") | x.Equals("G1") | x.Equals("G2") | x.Equals("G3"))).FirstOrDefault() != null ? true : iscoordinatedmotion;   //G0=rapid motion G1, G2, G3=coordinated motion
            _gcodeline.IsLinearMotion = islinearmotion = gcodechunks.Where(x => (x.Equals("G01") | x.Equals("G1") | x.Equals("G00") | x.Equals("G0"))).FirstOrDefault() != null ? true : gcodechunks.Where(x => (x.Equals("G02") | x.Equals("G03") | x.Equals("G2") | x.Equals("G3"))).FirstOrDefault() != null ? false : islinearmotion; //G2= CW ARC G3=CCW ARC
            _gcodeline.IsCW = iscw = islinearmotion ? null : (bool?)(gcodechunks.Where(x => (x.Equals("G02") | x.Equals("G2"))).FirstOrDefault() != null ? true : false);

            _gcodeline.Plane = gcodechunks.Where(x => x.Equals("G17")).FirstOrDefault() != null ? Plane.XY :
                gcodechunks.Where(x => x.Equals("G18")).FirstOrDefault() != null ? Plane.XZ :
                gcodechunks.Where(x => x.Equals("G19")).FirstOrDefault() != null ? Plane.YZ : plane;

            _gcodeline.IsMetric = ismetric = gcodechunks.Where(x => x.Equals("G20")).FirstOrDefault() != null ? false : ismetric;  //G20 = inch G21=metric
            _gcodeline.Isabsolutedistance = isabsolutedistance = gcodechunks.Where(x => x.Equals("G91")).FirstOrDefault() != null ? false : gcodechunks.Where(x => x.Equals("G90")).FirstOrDefault() != null ? true : isabsolutedistance;  //G91=incremental G90=absoluut

            _gcodeline.Xfrom = xfrom;
            _gcodeline.Xto = xto = gcodechunks.Where(x => (x.StartsWith("X"))).FirstOrDefault() != null ? (isabsolutedistance ? double.Parse(gcodechunks.Where(x => (x.StartsWith("X"))).First().Substring(1)) : xto + double.Parse(gcodechunks.Where(x => (x.StartsWith("X"))).First().Substring(1))) : xto;
            xfrom = xto;

            _gcodeline.Yfrom = yfrom;
            _gcodeline.Yto = yto = gcodechunks.Where(y => (y.StartsWith("Y"))).FirstOrDefault() != null ? (isabsolutedistance ? double.Parse(gcodechunks.Where(y => (y.StartsWith("Y"))).First().Substring(1)) : yto + double.Parse(gcodechunks.Where(y => (y.StartsWith("Y"))).First().Substring(1))) : yto;
            yfrom = yto;

            _gcodeline.Zfrom = zfrom;
            _gcodeline.Zto = zto = gcodechunks.Where(z => (z.StartsWith("Z"))).FirstOrDefault() != null ? (isabsolutedistance ? double.Parse(gcodechunks.Where(z => (z.StartsWith("Z"))).First().Substring(1)) : zto + double.Parse(gcodechunks.Where(z => (z.StartsWith("Z"))).First().Substring(1))) : zto;
            zfrom = zto;

            _gcodeline.I = i = gcodechunks.Where(x => (x.StartsWith("I"))).FirstOrDefault() != null ? double.Parse(gcodechunks.Where(x => (x.StartsWith("I"))).First().Substring(1)) : i;
            _gcodeline.J = j = gcodechunks.Where(x => (x.StartsWith("J"))).FirstOrDefault() != null ? double.Parse(gcodechunks.Where(x => (x.StartsWith("J"))).First().Substring(1)) : j;
            _gcodeline.K = k = gcodechunks.Where(x => (x.StartsWith("K"))).FirstOrDefault() != null ? double.Parse(gcodechunks.Where(x => (x.StartsWith("K"))).First().Substring(1)) : k;
            _gcodeline.P = p = gcodechunks.Where(x => (x.StartsWith("P"))).FirstOrDefault() != null ? double.Parse(gcodechunks.Where(x => (x.StartsWith("P"))).First().Substring(1)) : p;
            _gcodeline.R = r = gcodechunks.Where(x => (x.StartsWith("R"))).FirstOrDefault() != null ? double.Parse(gcodechunks.Where(x => (x.StartsWith("R"))).First().Substring(1)) : r;

            if (!_gcodeline.IsLinearMotion) //then it's a circulair motion 
            {
                switch (_gcodeline.Plane)
                {
                    case Plane.XY:
                        {
                            _gcodeline.Xcenter = _gcodeline.Xfrom + (_gcodeline.I != double.NaN ? _gcodeline.I : 0);
                            _gcodeline.Ycenter = _gcodeline.Yfrom + (_gcodeline.J != double.NaN ? _gcodeline.J : 0);
                            _gcodeline.Zcenter = _gcodeline.Zfrom;
                        }
                        break;
                    case Plane.XZ:
                        {
                            _gcodeline.Xcenter = _gcodeline.Xfrom + (_gcodeline.I != double.NaN ? _gcodeline.I : 0);
                            _gcodeline.Ycenter = _gcodeline.Yfrom;
                            _gcodeline.Zcenter = _gcodeline.Zfrom + (_gcodeline.K != double.NaN ? _gcodeline.K : 0);
                        }
                        break;
                    case Plane.YZ:
                        {
                            _gcodeline.Xcenter = _gcodeline.Xfrom;
                            _gcodeline.Ycenter = _gcodeline.Yfrom + (_gcodeline.J != double.NaN ? _gcodeline.J : 0);
                            _gcodeline.Zcenter = _gcodeline.Zfrom + (_gcodeline.K != double.NaN ? _gcodeline.K : 0);
                        }
                        break;
                }
            }
            else //a linear 
            {
                _gcodeline.Xcenter = double.NaN;
                _gcodeline.Ycenter = double.NaN;
                _gcodeline.Zcenter = double.NaN;
            }

            _gcodeline.LineNumber = gcodechunks.Where(x => (x.StartsWith("N"))).FirstOrDefault();
            _gcodeline.Raw = _gcodeline.LineNumber != null ? _gcodeline.Raw.Replace(_gcodeline.LineNumber, "").TrimStart() : _gcodeline.Raw.TrimStart(); ;
            _gcodeline.GrblCommand = _gcodeline.Raw.Replace(" ", "");
            _gcodeline.SerialBufferLength = _gcodeline.GrblCommand.Length + 1;  //Because "\r" is gonna be added.

            MinX = xto < MinX ? xto : MinX;
            MinY = yto < MinY ? yto : MinY;
            MinZ = zto < MinZ ? zto : MinZ;

            MaxX = xto > MaxX ? xto : MaxX;
            MaxY = yto > MaxY ? yto : MaxY;
            MaxZ = zto > MaxZ ? zto : MaxZ;
        }

        #endregion
    }
}
