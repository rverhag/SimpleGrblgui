using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using Vhr.Gcode;
using VhR.SimpleGrblGui.Classes;
using Vector3 = SharpDX.Vector3;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class ViewportViewModel :  ObservableObject, IDisposable
    {
        private LineBuilder localprocessed = new LineBuilder();

        private double X0 { get { return App.Grbl.WorkCoordinates.Current.X; } }
        private double Y0 { get { return App.Grbl.WorkCoordinates.Current.Y; } }
        private double Z0 { get { return App.Grbl.WorkCoordinates.Current.Z; } }

        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetValue(ref title, value, "Title");
            }
        }

        private string subTitle;
        public string SubTitle
        {
            get
            {
                return subTitle;
            }
            set
            {
                SetValue(ref subTitle, value, "SubTitle");
            }
        }

        private Camera camera;
        public Camera Camera
        {
            get
            {
                return camera;
            }

            protected set
            {
                SetValue(ref camera, value, "Camera");
                //CameraModel = value is PerspectiveCamera
                //                       ? Perspective
                //                       : value is OrthographicCamera ? Orthographic : null;
            }
        }

        private IEffectsManager effectsManager;
        public IEffectsManager EffectsManager
        {
            get { return effectsManager; }
            protected set
            {
                SetValue(ref effectsManager, value);
            }
        }

        private IRenderTechnique renderTechnique;
        public IRenderTechnique RenderTechnique
        {
            get
            {
                return renderTechnique;
            }
            set
            {
                SetValue(ref renderTechnique, value, "RenderTechnique");
            }
        }

        private LineGeometry3D machinespace;
        public LineGeometry3D MachineSpace
        {
            get
            {
                return machinespace;
            }
            set
            {
                SetValue(ref machinespace, value, "MachineSpace");
            }
        }

        private LineGeometry3D workspace;
        public LineGeometry3D WorkSpace
        {
            get
            {
                return workspace;
            }
            set
            {
                SetValue(ref workspace, value, "WorkSpace");
            }
        }

        private LineGeometry3D routercut;
        public LineGeometry3D RouterCut
        {
            get
            {
                return routercut;
            }
            set
            {
                SetValue(ref routercut, value, "RouterCut");
            }
        }

        private LineGeometry3D routermove;
        public LineGeometry3D RouterMove
        {
            get
            {
                return routermove;
            }
            set
            {
                SetValue(ref routermove, value, "RouterMove");
            }
        }

        private LineGeometry3D processed;
        public LineGeometry3D Processed
        {
            get
            {
                return processed;
            }
            set
            {
                SetValue(ref processed, value, "Processed");
            }
        }

        private LineGeometry3D currentposition;
        public LineGeometry3D CurrentPosition
        {
            get
            {
                return currentposition;
            }
            set
            {
                SetValue(ref currentposition, value, "CurrentPosition");
            }
        }

        public ViewportViewModel()
        {
            App.Grbl.GcodeChanged += Grbl_GcodeChanged;
            App.Grbl.CoordinateChanged += Grbl_CoordinateChanged;
            App.Grbl.GcodeLineChanged += Grbl_GcodeLineChanged;
            App.Grbl.IsReset += Grbl_IsReset;

            Camera = new PerspectiveCamera
            {
                //https://gamedev.stackexchange.com/questions/19774/determine-corners-of-a-specific-plane-in-the-frustum
                Position = new System.Windows.Media.Media3D.Point3D(200, -1500, 750),
                LookDirection = new Vector3D(-200, 1500, -750),
                UpDirection = new Vector3D(0.54, 0.159, 2.166),
                NearPlaneDistance = 1,
                FarPlaneDistance = 30000,
                CreateLeftHandSystem = false,
                FieldOfView=45
            };

            EffectsManager = new DefaultEffectsManager();
            RenderTechnique = EffectsManager[DefaultRenderTechniqueNames.Volume3D];

            CreateMachinespace();
        }

        private void Grbl_IsReset(object sender, EventArgs e)
        {
            localprocessed = new LineBuilder();
            Processed = null;
        }

        private void Grbl_GcodeLineChanged(object sender, EventArgs e)
        {
            GcodeLine gcodeline = (GcodeLine)sender;

            Vector3 frompoint = new Vector3((float)(X0 + gcodeline.Xfrom), (float)(Y0 + gcodeline.Yfrom), (float)(Z0 + gcodeline.Zfrom));
            Vector3 topoint = new Vector3((float)(X0 + gcodeline.Xto), (float)(Y0 + gcodeline.Yto), (float)(Z0 + gcodeline.Zto));
            Vector3 centerpoint = new Vector3((float)(X0 + gcodeline.Xcenter), (float)(Y0 + gcodeline.Ycenter), (float)(Z0 + gcodeline.Zcenter));

            if (!float.IsNaN(frompoint.X) & !float.IsNaN(frompoint.Y) & !float.IsNaN(frompoint.Z) & !float.IsNaN(topoint.X) & !float.IsNaN(topoint.Y) & !float.IsNaN(topoint.Z))
            {
                if (gcodeline.IsCuttingMotion)
                {
                    if (gcodeline.IsLinearMotion)
                    {
                        localprocessed.AddLine(frompoint, topoint);
                    }
                    else
                    {
                        ArcPointSegment(ref localprocessed, frompoint, topoint, centerpoint, (bool)gcodeline.IsCW);
                    }
                }
                else
                {
                    if (gcodeline.IsLinearMotion)
                    {
                        localprocessed.AddLine(frompoint, topoint);
                    }
                    else
                    {
                        ArcPointSegment(ref localprocessed, frompoint, topoint, centerpoint, (bool)gcodeline.IsCW);
                    }
                }
            }

            Processed = localprocessed.ToLineGeometry3D();
        }

        private void Grbl_CoordinateChanged(object sender, EventArgs e)
        {
            var box = new LineBuilder();
            box.AddBox(new Vector3((float)App.Grbl.MPOS.X, (float)App.Grbl.MPOS.Y, (float)App.Grbl.MPOS.Z + 10), 1, 1, 20);
            CurrentPosition = box.ToLineGeometry3D();
        }

        private void Grbl_GcodeChanged(object sender, System.EventArgs e)
        {
            Title = App.Grbl.Gcode.FileName;
            SubTitle = string.Format("Number of lines: {0}", App.Grbl.Gcode.Count);

            localprocessed = new LineBuilder();
            Processed = null;
            WorkSpace = null;
            MachineSpace = null;
            RouterCut = null;
            RouterMove = null;

            CreateMachinespace();
            CreateWorkspace();
            CreatRouterPath();
        }

        private void CreateMachinespace()
        {
            var machinespace = new LineBuilder();

            Vector3 p0machine = new Vector3(0, 0, 0);
            Vector3 p1machine = new Vector3(0 - (float)App.Grbl.MaxXDistance, 0, 0);
            Vector3 p2machine = new Vector3(0 - (float)App.Grbl.MaxXDistance, 0 - (float)App.Grbl.MaxYDistance, 0);
            Vector3 p3machine = new Vector3(0, 0 - (float)App.Grbl.MaxYDistance, 0);
            Vector3 p4machine = new Vector3(0, 0, 0 - (float)App.Grbl.MaxZDistance);
            Vector3 p5machine = new Vector3(0 - (float)App.Grbl.MaxXDistance, 0, 0 - (float)App.Grbl.MaxZDistance);
            Vector3 p6machine = new Vector3(0 - (float)App.Grbl.MaxXDistance, 0 - (float)App.Grbl.MaxYDistance, 0 - (float)App.Grbl.MaxZDistance);
            Vector3 p7machine = new Vector3(0, 0 - (float)App.Grbl.MaxYDistance, 0 - (float)App.Grbl.MaxZDistance);

            // machinespacelines.AddBox(new Vector3(0, 0, 0), App.Grbl.MaxXDistance, App.Grbl.MaxYDistance, App.Grbl.MaxZDistance);

            machinespace.AddLine(p0machine, p1machine);
            machinespace.AddLine(p0machine, p1machine);
            machinespace.AddLine(p1machine, p2machine);
            machinespace.AddLine(p2machine, p3machine);
            machinespace.AddLine(p3machine, p0machine);
            machinespace.AddLine(p4machine, p5machine);
            machinespace.AddLine(p5machine, p6machine);
            machinespace.AddLine(p6machine, p7machine);
            machinespace.AddLine(p7machine, p4machine);
            machinespace.AddLine(p0machine, p4machine);
            machinespace.AddLine(p1machine, p5machine);
            machinespace.AddLine(p2machine, p6machine);
            machinespace.AddLine(p3machine, p7machine);

            MachineSpace = machinespace.ToLineGeometry3D();
        }

        private void CreateWorkspace()
        {
            var workspace = new LineBuilder();

            Vector3 p0work = new Vector3((float)(X0 + App.Grbl.Gcode.MinX), (float)(Y0 + App.Grbl.Gcode.MinY), (float)(Z0 + App.Grbl.Gcode.MinZ));
            Vector3 p1work = new Vector3((float)(p0work.X + App.Grbl.Gcode.DeltaX), p0work.Y, p0work.Z);
            Vector3 p2work = new Vector3((float)(p0work.X + App.Grbl.Gcode.DeltaX), (float)(p0work.Y + App.Grbl.Gcode.DeltaY), p0work.Z);
            Vector3 p3work = new Vector3(p0work.X, (float)(p0work.Y + App.Grbl.Gcode.DeltaY), p0work.Z);
            Vector3 p4work = new Vector3(p0work.X, p0work.Y, (float)(p0work.Z + App.Grbl.Gcode.DeltaZ));
            Vector3 p5work = new Vector3((float)(p0work.X + App.Grbl.Gcode.DeltaX), p0work.Y, (float)(p0work.Z + App.Grbl.Gcode.DeltaZ));
            Vector3 p6work = new Vector3((float)(p0work.X + App.Grbl.Gcode.DeltaX), (float)(p0work.Y + App.Grbl.Gcode.DeltaY), (float)(p0work.Z + App.Grbl.Gcode.DeltaZ));
            Vector3 p7work = new Vector3(p0work.X, (float)(p0work.Y + App.Grbl.Gcode.DeltaY), (float)(p0work.Z + App.Grbl.Gcode.DeltaZ));


            workspace.AddLine(p0work, p1work);
            workspace.AddLine(p0work, p1work);
            workspace.AddLine(p1work, p2work);
            workspace.AddLine(p2work, p3work);
            workspace.AddLine(p3work, p0work);
            workspace.AddLine(p4work, p5work);
            workspace.AddLine(p5work, p6work);
            workspace.AddLine(p6work, p7work);
            workspace.AddLine(p7work, p4work);
            workspace.AddLine(p0work, p4work);
            workspace.AddLine(p1work, p5work);
            workspace.AddLine(p2work, p6work);
            workspace.AddLine(p3work, p7work);

            WorkSpace = workspace.ToLineGeometry3D();
        }

        private void CreatRouterPath()
        {
            LineBuilder routercut = new LineBuilder();
            LineBuilder routermove = new LineBuilder();

            foreach (GcodeLine gcodeline in App.Grbl.Gcode)
            {
                Vector3 frompoint = new Vector3((float)(X0 + gcodeline.Xfrom), (float)(Y0 + gcodeline.Yfrom), (float)(Z0 + gcodeline.Zfrom));
                Vector3 topoint = new Vector3((float)(X0 + gcodeline.Xto), (float)(Y0 + gcodeline.Yto), (float)(Z0 + gcodeline.Zto));
                Vector3 centerpoint = new Vector3((float)(X0 + gcodeline.Xcenter), (float)(Y0 + gcodeline.Ycenter), (float)(Z0 + gcodeline.Zcenter));

                if (!float.IsNaN(frompoint.X) & !float.IsNaN(frompoint.Y) & !float.IsNaN(frompoint.Z) & !float.IsNaN(topoint.X) & !float.IsNaN(topoint.Y) & !float.IsNaN(topoint.Z))
                {
                    if (gcodeline.IsCuttingMotion)
                    {
                        if (gcodeline.IsLinearMotion)
                        {
                           routercut.AddLine(frompoint, topoint);
                        }
                        else
                        {
                            ArcPointSegment(ref routercut, frompoint, topoint, centerpoint, (bool)gcodeline.IsCW);
                        }
                    }
                    else
                    {
                        if (gcodeline.IsLinearMotion)
                        {
                            routermove.AddLine(frompoint, topoint);
                        }
                        else
                        {
                            ArcPointSegment(ref routermove, frompoint, topoint, centerpoint, (bool)gcodeline.IsCW);
                        }
                    }
                }
            }
            RouterCut= routercut.ToLineGeometry3D();
            RouterMove = routermove.ToLineGeometry3D();
        }

        private void ArcPointSegment(ref LineBuilder _routerpath, Vector3 startpoint, Vector3 endpoint, Vector3 centerpoint, bool clockwise)
        {
            int numberoflinesinsegment = 5;

            float radius = Radius(centerpoint, startpoint);
            float startangle = Angle(centerpoint, startpoint, true);
            float endangle = Angle(centerpoint, endpoint, true);

            endangle = endangle == 0 ? (float)Math.PI * 2 : endangle;

            float arclength = (!clockwise && endangle < startangle) ? (float)((Math.PI * 2 - startangle) + endangle) : ((clockwise && endangle > startangle) ? (float)((Math.PI * 2 - endangle) + startangle) : (float)Math.Abs(endangle - startangle));

            Vector3 endoflinesegment = endpoint;
            
            float angle;
            float dz = (endpoint.Z - startpoint.Z) / numberoflinesinsegment;


            List<Vector3> vectors = new List<Vector3>();

            for (int i = 0; i < numberoflinesinsegment; i++)
            {
                if (i > 1)
                {
                    vectors.Add(endoflinesegment);
                }

                angle = clockwise ? (startangle - i * arclength / numberoflinesinsegment) : (startangle + i * arclength / numberoflinesinsegment);
                angle = (angle >= Math.PI * 2) ? angle - (float)Math.PI * 2 : angle;

                endoflinesegment.X = (float)Math.Cos(angle) * radius + centerpoint.X;
                endoflinesegment.Y = (float)Math.Sin(angle) * radius + centerpoint.Y;
                endoflinesegment.Z += dz;

                vectors.Add(endoflinesegment);
            }

            vectors.Add(endoflinesegment);
            vectors.Add(endpoint);


            for (int x=1; x<vectors.Count; x++)
            {
                _routerpath.AddLine(vectors[x - 1], vectors[x]);
            }
        }

        private float Radius(Vector3 centerpoint, Vector3 point)
        {
            double dx = point.X - centerpoint.X;
            double dy = point.Y - centerpoint.Y;

            return (float)Math.Sqrt(Math.Pow(dx, 2.0) + Math.Pow(dy, 2.0));
        }
        private float Angle(Vector3 centerpoint, Vector3 point, bool inradians)
        {
            double dX = point.X - centerpoint.X;
            double dY = point.Y - centerpoint.Y;
            double angle;
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

            return inradians ? (float)angle : (float)(angle * (180.0 / Math.PI));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (EffectsManager != null)
                {
                    var effectManager = EffectsManager as IDisposable;
                    Disposer.RemoveAndDispose(ref effectManager);
                }
                disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~ViewportViewModel()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
