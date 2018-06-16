using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Vhr;
using Vhr.Gcode;
using VhR.SimpleGrblGui.ViewModels;

namespace VhR.SimpleGrblGui.Usercontrols
{
    public partial class DrawingControl : UserControl
    {
        public readonly double inbufferlinethickness = 4d;
        public readonly double errorlinethickness = 3d;
        public readonly double cuttinglinethickness = 2d;
        public readonly double motionlinethickness = 1d;
        public readonly double processedlinethickness = 0.5d;
        public readonly double coordpointthickness = 8d;

        public readonly Color machinespacecolor = Brushes.Gray.Color;
        public readonly Color workspacecolor = Brushes.LightGray.Color;
        public readonly Color cuttingspacecolor = Brushes.Gray.Color;
        public readonly Color cuttingcolor = Brushes.Blue.Color;
        public readonly Color motioncolor = Brushes.IndianRed.Color;
        public readonly Color inbuffercolor = Brushes.Yellow.Color;
        public readonly Color errorcolor = Brushes.Red.Color;
        public readonly Color processedcolor = Brushes.Gray.Color;
        public readonly Color coordpointcolor = Brushes.Black.Color;

        private Grbl grbl;

        public DrawingControl()
        {
            InitializeComponent();
            grbl = Grbl.Interface;

            Viewport3D.DefaultCamera = PerspectiveCamera;
            Viewport3D.LookAt(new Point3D(-220, -456, -19));

            grbl.CoordinateChanged += Grbl_CoordinateChanged;
            grbl.GcodeChanged += Grbl_GcodeChanged;
            grbl.GcodeLineChanged += Grbl_GcodeLineChangedAsync;
            grbl.CurrentWorkCoordinateChanged += Grbl_CurrentWorkCoordinateChanged;
            grbl.IsReset += Grbl_IsReset;
        }

        private void Grbl_IsReset(object sender, EventArgs e)
        {
            if (grbl.Gcode != null)
                Grbl_GcodeChanged(sender, e);
        }

        private async void Grbl_GcodeLineChangedAsync(object sender, EventArgs e)
        {
            GcodeLine gcodeline = (GcodeLine)sender;
            await Task.Run(() => Dispatcher.BeginInvoke(new Action(delegate ()
                      {
                          var drawingsegments = RouterPath.Children.Where(x => (x is DrawingSegment) ? true : false).Select(x => x).ToList();
                          var item = (drawingsegments != null && drawingsegments.Count > 0) ? drawingsegments.Where(x => ((DrawingSegment)x).Index.Equals(gcodeline.Index)).Select(x => x).FirstOrDefault() : null;
                          if (item != null)
                          {
                              ((DrawingSegment)item).Color = gcodeline.InSerialBuffer ? inbuffercolor : (bool)gcodeline.response?.Equals("ok") ? processedcolor : gcodeline.IsCuttingMotion ? cuttingcolor : motioncolor;
                              ((DrawingSegment)item).Thickness = gcodeline.InSerialBuffer ? inbufferlinethickness : gcodeline.IsCuttingMotion ? cuttinglinethickness : motionlinethickness;
                          }
                      }
                   )));
        }

        private void Grbl_CurrentWorkCoordinateChanged(object sender, EventArgs e)
        {
            if (grbl.Gcode != null) Grbl_GcodeChanged(sender, e);
        }

        public PerspectiveCamera PerspectiveCamera { get; private set; } = new PerspectiveCamera()
        {
            LookDirection = new Vector3D(-1000, 700, -1800),
            UpDirection = new Vector3D(0.54, 0.159, 2.166),
            Position = new Point3D(771, -1140, 1731),
           
        };

        private void Grbl_GcodeChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
                                        {
                                            Viewport3D.Children.Clear();
                                        }
                                    ));

            CreateMachinespace();
            CreateTotalWorkspace();
            CreateTotalCuttingspace();
            CreateRouterPath();
        }

        private BoundingBoxWireFrameVisual3D TotalWorkspace { get; set; }
        private BoundingBoxWireFrameVisual3D Machinespace { get; set; }
        private BoundingBoxWireFrameVisual3D TotalCuttingspace { get; set; }
        private ModelVisual3D RouterPath { get; set; }
        private double X0 { get { return grbl.WorkCoordinates.Current.X; } }
        private double Y0 { get { return grbl.WorkCoordinates.Current.Y; } }
        private double Z0 { get { return grbl.WorkCoordinates.Current.Z; } }
       
        private async void Viewport3D_PreviewMouseRightButtonDownAsync(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RouterPath != null && Viewport3D.Children.Contains(RouterPath))
            {
                await Task.Run(() =>
                        Dispatcher.BeginInvoke(new Action(delegate ()
                        {
                            Viewport3D.Children.Remove(RouterPath);
                        }
                        )));
            }
        }

        private async void Viewport3D_PreviewMouseRightButtonUpAsync(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RouterPath != null && !Viewport3D.Children.Contains(RouterPath))
            {
                await Task.Run(() =>
                      Dispatcher.BeginInvoke(new Action(delegate ()
                      {
                          Viewport3D.Children.Add(RouterPath);
                      }
                      )));
            }
        }

        private List<Point3D> CoordinatePoints = new List<Point3D>();
        private int maxnumberofpoints = 1;
        private void Grbl_CoordinateChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                CoordinatePoints.Add(new Point3D(grbl.MPOS.X, grbl.MPOS.Y, grbl.MPOS.Z));

                if (CoordinatePoints.Count > maxnumberofpoints) CoordinatePoints.RemoveAt(0);

                //try to locate the segment in the viewport
                var foundpoints = Viewport3D.Children.Where(x => (x is PointsVisual3D) ? true : false).Select(x => x).ToList();
                var item = (foundpoints != null && foundpoints.Count > 0) ? foundpoints.Where(x => ((PointsVisual3D)x).GetName().Equals("previouspoints")).Select(x => x).FirstOrDefault() : null;

                if (item != null && item is PointsVisual3D) Viewport3D.Children.Remove(item);

                PointsVisual3D points = new PointsVisual3D();
                points.SetName("previouspoints");

                foreach (Point3D point in CoordinatePoints) points.Points.Add(point);

                points.Size = coordpointthickness;
                points.Color = coordpointcolor;
                Viewport3D.Children.Add(points);
            }));
        }

        private void CreateMachinespace()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {

                Point3D p0machine = new Point3D(0, 0, 0);
                Point3D p1machine = new Point3D(0 - grbl.MaxXDistance, 0, 0);
                Point3D p2machine = new Point3D(0 - grbl.MaxXDistance, 0 - grbl.MaxYDistance, 0);
                Point3D p3machine = new Point3D(0, 0 - grbl.MaxYDistance, 0);
                Point3D p4machine = new Point3D(0, 0, 0 - grbl.MaxZDistance);
                Point3D p5machine = new Point3D(0 - grbl.MaxXDistance, 0, 0 - grbl.MaxZDistance);
                Point3D p6machine = new Point3D(0 - grbl.MaxXDistance, 0 - grbl.MaxYDistance, 0 - grbl.MaxZDistance);
                Point3D p7machine = new Point3D(0, 0 - grbl.MaxYDistance, 0 - grbl.MaxZDistance);


                Machinespace = new BoundingBoxWireFrameVisual3D
                {
                    Color = machinespacecolor
                };

                Machinespace.Points.Add(p0machine);
                Machinespace.Points.Add(p1machine);

                Machinespace.Points.Add(p1machine);
                Machinespace.Points.Add(p2machine);

                Machinespace.Points.Add(p2machine);
                Machinespace.Points.Add(p3machine);

                Machinespace.Points.Add(p3machine);
                Machinespace.Points.Add(p0machine);

                Machinespace.Points.Add(p4machine);
                Machinespace.Points.Add(p5machine);

                Machinespace.Points.Add(p5machine);
                Machinespace.Points.Add(p6machine);

                Machinespace.Points.Add(p6machine);
                Machinespace.Points.Add(p7machine);

                Machinespace.Points.Add(p7machine);
                Machinespace.Points.Add(p4machine);

                Machinespace.Points.Add(p0machine);
                Machinespace.Points.Add(p4machine);

                Machinespace.Points.Add(p1machine);
                Machinespace.Points.Add(p5machine);

                Machinespace.Points.Add(p2machine);
                Machinespace.Points.Add(p6machine);

                Machinespace.Points.Add(p3machine);
                Machinespace.Points.Add(p7machine);

                Viewport3D.Children.Add(Machinespace);
            }));
        }
        private void CreateTotalWorkspace()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {

                Point3D p0work = new Point3D(X0 + grbl.Gcode.MinX, Y0 + grbl.Gcode.MinY, Z0 + grbl.Gcode.MinZ);
                Point3D p1work = new Point3D(p0work.X + grbl.Gcode.DeltaX, p0work.Y, p0work.Z);
                Point3D p2work = new Point3D(p0work.X + grbl.Gcode.DeltaX, p0work.Y + grbl.Gcode.DeltaY, p0work.Z);
                Point3D p3work = new Point3D(p0work.X, p0work.Y + grbl.Gcode.DeltaY, p0work.Z);
                Point3D p4work = new Point3D(p0work.X, p0work.Y, p0work.Z + grbl.Gcode.DeltaZ);
                Point3D p5work = new Point3D(p0work.X + grbl.Gcode.DeltaX, p0work.Y, p0work.Z + grbl.Gcode.DeltaZ);
                Point3D p6work = new Point3D(p0work.X + grbl.Gcode.DeltaX, p0work.Y + grbl.Gcode.DeltaY, p0work.Z + grbl.Gcode.DeltaZ);
                Point3D p7work = new Point3D(p0work.X, p0work.Y + grbl.Gcode.DeltaY, p0work.Z + grbl.Gcode.DeltaZ);

                TotalWorkspace = new BoundingBoxWireFrameVisual3D
                {
                    Color = workspacecolor
                };

                TotalWorkspace.Points.Add(p0work);
                TotalWorkspace.Points.Add(p1work);

                TotalWorkspace.Points.Add(p1work);
                TotalWorkspace.Points.Add(p2work);

                TotalWorkspace.Points.Add(p2work);
                TotalWorkspace.Points.Add(p3work);

                TotalWorkspace.Points.Add(p3work);
                TotalWorkspace.Points.Add(p0work);

                TotalWorkspace.Points.Add(p4work);
                TotalWorkspace.Points.Add(p5work);

                TotalWorkspace.Points.Add(p5work);
                TotalWorkspace.Points.Add(p6work);

                TotalWorkspace.Points.Add(p6work);
                TotalWorkspace.Points.Add(p7work);

                TotalWorkspace.Points.Add(p7work);
                TotalWorkspace.Points.Add(p4work);

                TotalWorkspace.Points.Add(p0work);
                TotalWorkspace.Points.Add(p4work);

                TotalWorkspace.Points.Add(p1work);
                TotalWorkspace.Points.Add(p5work);

                TotalWorkspace.Points.Add(p2work);
                TotalWorkspace.Points.Add(p6work);

                TotalWorkspace.Points.Add(p3work);
                TotalWorkspace.Points.Add(p7work);

                Viewport3D.Children.Add(TotalWorkspace);
            }));
        }
        private void CreateTotalCuttingspace()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                Point3D p0material = new Point3D(X0 + grbl.Gcode.MinX, Y0 + grbl.Gcode.MinY, Z0 + 0);
                Point3D p1material = new Point3D(p0material.X + grbl.Gcode.DeltaX, p0material.Y, p0material.Z);
                Point3D p2material = new Point3D(p0material.X + grbl.Gcode.DeltaX, p0material.Y + grbl.Gcode.DeltaY, p0material.Z);
                Point3D p3material = new Point3D(p0material.X, p0material.Y + grbl.Gcode.DeltaY, p0material.Z);

                TotalCuttingspace = new BoundingBoxWireFrameVisual3D()
                {
                    Color = cuttingspacecolor,
                };

                TotalCuttingspace.Points.Add(p0material);
                TotalCuttingspace.Points.Add(p1material);

                TotalCuttingspace.Points.Add(p1material);
                TotalCuttingspace.Points.Add(p2material);

                TotalCuttingspace.Points.Add(p2material);
                TotalCuttingspace.Points.Add(p3material);

                TotalCuttingspace.Points.Add(p3material);
                TotalCuttingspace.Points.Add(p0material);

                Viewport3D.Children.Add(TotalCuttingspace);
            }));
        }
        private void CreateRouterPath()
        {
            Dispatcher.BeginInvoke(new Action(delegate ()
            {
                RouterPath = new ModelVisual3D();

                foreach (GcodeLine gcodeline in grbl.Gcode)
                {
                    Point3D frompoint = new Point3D(X0 + gcodeline.Xfrom, Y0 + gcodeline.Yfrom, Z0 + gcodeline.Zfrom);
                    Point3D topoint = new Point3D(X0 + gcodeline.Xto, Y0 + gcodeline.Yto, Z0 + gcodeline.Zto);
                    Point3D centerpoint = new Point3D(X0 + gcodeline.Xcenter, Y0 + gcodeline.Ycenter, Z0 + gcodeline.Zcenter);

                    DrawingSegment drawingsegment = gcodeline.IsLinearMotion ? LinePointSegment(gcodeline.Index, frompoint, topoint) : ArcPointSegment(gcodeline.Index, frompoint, topoint, centerpoint, (bool)gcodeline.IsCW);
                    drawingsegment.Color = gcodeline.IsCuttingMotion ? cuttingcolor : motioncolor;
                    drawingsegment.Thickness = gcodeline.IsCuttingMotion ? cuttinglinethickness : motionlinethickness;

                    RouterPath.Children.Add(drawingsegment);
                }

                Viewport3D.Children.Add(RouterPath);
            }));
        }

        private double Radius(Point3D centerpoint, Point3D point)
        {
            double dx = point.X - centerpoint.X;
            double dy = point.Y - centerpoint.Y;

            return Math.Sqrt(Math.Pow(dx, 2.0) + Math.Pow(dy, 2.0));
        }
        private double Angle(Point3D centerpoint, Point3D point, bool inradians)
        {
            double dX = point.X - centerpoint.X;
            double dY = point.Y - centerpoint.Y;

            double angle = 0.0;

            if (dX != 0)
            {
                if (dX > 0) //Rigth side 270 - 90  (Q1 or Q4)?
                {
                    if (dY >= 0) //Q1 0-90
                    {
                        angle = Math.Atan(dY / dX);
                    }
                    else //Q4 270 - 360
                    {
                        angle = Math.PI * 2 - Math.Abs(Math.Atan(dY / dX));
                    }
                }
                else //it's the left side  90 - 270 (Q2 or Q3)
                {
                    if (dY >= 0) //Q2 //90 -180
                    {
                        angle = Math.PI - Math.Abs(Math.Atan(dY / dX));
                    }
                    else //Q3 // 180 -270
                    {
                        angle = Math.PI + Math.Abs(Math.Atan(dY / dX));
                    }
                }
            }
            else //on the Y-axe
            {
                //Positive part (between Q1 and Q2 at pi/2)
                if (dY > 0)
                {
                    angle = Math.PI / 2.0;
                }
                //Negative part (between Q3 and Q4 at 1 1/2 pi)
                else
                {
                    angle = Math.PI * 3.0 / 2.0;
                }
            }

            return inradians ? angle : angle * (180.0 / Math.PI);
        }
        private DrawingSegment LinePointSegment(int _index, Point3D from, Point3D to)
        {
            DrawingSegment drawingsegment = new DrawingSegment(_index);
            drawingsegment.Points.Add(from);
            drawingsegment.Points.Add(to);
            return drawingsegment;
        }
        private DrawingSegment ArcPointSegment(int _index, Point3D startpoint, Point3D endpoint, Point3D centerpoint, bool clockwise)
        {
            DrawingSegment drawingsegment = new DrawingSegment(_index);

            int numberoflinesinsegment = 5;

            double radius = Radius(centerpoint, startpoint);
            double startangle = Angle(centerpoint, startpoint, true);
            double endangle = Angle(centerpoint, endpoint, true);

            endangle = endangle == 0 ? Math.PI * 2 : endangle;

            double arclength = (!clockwise && endangle < startangle) ? ((Math.PI * 2 - startangle) + endangle) : ((clockwise && endangle > startangle) ? ((Math.PI * 2 - endangle) + startangle) : Math.Abs(endangle - startangle));

            Point3D endoflinesegment = endpoint;


            double angle;
            double dz = (endpoint.Z - startpoint.Z) / numberoflinesinsegment;

            for (int i = 0; i < numberoflinesinsegment; i++)
            {
                if (i > 1)
                {
                    drawingsegment.Points.Add(endoflinesegment);
                }

                angle = clockwise ? (startangle - i * arclength / numberoflinesinsegment) : (startangle + i * arclength / numberoflinesinsegment);
                angle = (angle >= Math.PI * 2) ? angle - Math.PI * 2 : angle;

                endoflinesegment.X = Math.Cos(angle) * radius + centerpoint.X;
                endoflinesegment.Y = Math.Sin(angle) * radius + centerpoint.Y;
                endoflinesegment.Z += dz;

                drawingsegment.Points.Add(endoflinesegment);
            }

            drawingsegment.Points.Add(endoflinesegment);
            drawingsegment.Points.Add(endpoint);

            return drawingsegment;
        }
    }

    public class DrawingSegment : LinesVisual3D
    {
        public DrawingSegment(int _index)
        {
            Index = _index;
        }
        public int Index { get; set; }
    }
}
