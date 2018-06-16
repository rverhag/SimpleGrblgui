using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vhr;
using WpfCap;

namespace VhR.SimpleGrblGui.Usercontrols
{
    public partial class CameraControl : UserControl
    {
        private Grbl grbl;
        private CapPlayer capplayer = null;
        private Image CenterLines = null;

        public CameraControl()
        {
            grbl = Grbl.Interface;
            InitializeComponent();

            ColorCombo.SelectionChanged += ColorCombo_SelectionChanged;
        }

        private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawGrid();
        }

        void capplayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGrid();
        }

        private void DrawGrid()
        {
            ImageGrid.Children.Remove(CenterLines);

            if (capplayer != null && CenterLines != null)
            {
                double videoheigth = capplayer.ActualHeight == 0 ? 2 : capplayer.ActualHeight;
                double videowidth = capplayer.ActualWidth == 0 ? 2 : capplayer.ActualWidth;

                double videoverticalhalf = videoheigth / 2;
                double videohorizontalhalf = videowidth / 2;

                LineGeometry horizontal = new LineGeometry(new Point(0, videoverticalhalf), new Point(videowidth, videoverticalhalf));
                LineGeometry vertical = new LineGeometry(new Point(videohorizontalhalf, 0), new Point(videohorizontalhalf, videoheigth));
                LineGeometry diagonal1 = new LineGeometry(new Point(videohorizontalhalf, 0), new Point(videohorizontalhalf, videoheigth))
                {
                    Transform = new RotateTransform(45, videohorizontalhalf, videoverticalhalf)
                };
                LineGeometry diagonal2 = new LineGeometry(new Point(videohorizontalhalf, 0), new Point(videohorizontalhalf, videoheigth))
                {
                    Transform = new RotateTransform(-45, videohorizontalhalf, videoverticalhalf)
                };
                EllipseGeometry center1 = new EllipseGeometry(new Point(videohorizontalhalf, videoverticalhalf), 15, 15);
                EllipseGeometry center2 = new EllipseGeometry(new Point(videohorizontalhalf, videoverticalhalf), 40, 40);
                EllipseGeometry center4 = new EllipseGeometry(new Point(videohorizontalhalf, videoverticalhalf), 80, 80);
                EllipseGeometry center3 = new EllipseGeometry(new Point(videohorizontalhalf, videoverticalhalf), videoverticalhalf, videoverticalhalf);

                GeometryDrawing horizontaldraw = new GeometryDrawing
                {
                    
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = horizontal
                };

                GeometryDrawing verticaldraw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = vertical
                };

                GeometryDrawing diagonal1draw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = diagonal1
                };

                GeometryDrawing diagonal2draw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = diagonal2
                };



                GeometryDrawing centerdraw1 = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = center1
                };

                GeometryDrawing centerdraw2 = new GeometryDrawing
                {
                    //Pen = new Pen(Brushes.Yellow, 0.5),
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = center2
                };
                GeometryDrawing centerdraw3 = new GeometryDrawing
                {
                    //Pen = new Pen(Brushes.Yellow, 0.5),
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = center3
                };

                GeometryDrawing centerdraw4 = new GeometryDrawing
                {
                    //Pen = new Pen(Brushes.Yellow, 0.5),
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = center4
                };

                DrawingGroup centerdrawing = new DrawingGroup();
                centerdrawing.Children.Add(horizontaldraw);
                centerdrawing.Children.Add(verticaldraw);
                centerdrawing.Children.Add(diagonal1draw);
                centerdrawing.Children.Add(diagonal2draw);

                if (videowidth > 0)
                {
                    centerdrawing.Children.Add(centerdraw1);
                    centerdrawing.Children.Add(centerdraw2);
                    centerdrawing.Children.Add(centerdraw3);
                    centerdrawing.Children.Add(centerdraw4);
                }

                CenterLines.Source = new DrawingImage(centerdrawing);

                ImageGrid.Children.Add(CenterLines);
            }
        }

        private void ImageGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            ImageGrid.Children.Remove(capplayer);
            ImageGrid.Children.Remove(CenterLines);

            CenterLines = null;

            if (capplayer != null)
            {
                capplayer.SizeChanged -= capplayer_SizeChanged;
                capplayer.Dispose();
                capplayer = null;
            }
        }

        private void ImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            CenterLines = new Image();
            PlaceCameraView();
            DrawGrid();
        }

        private void PlaceCameraView()
        {
            capplayer = new CapPlayer
            {
               // Margin = new Thickness(15)
            };
            // capplayer.Framerate = 2;

            ImageGrid.Children.Add(capplayer);

            CenterLines.BringIntoView();
            capplayer.SizeChanged += capplayer_SizeChanged;
        }
    }
}
