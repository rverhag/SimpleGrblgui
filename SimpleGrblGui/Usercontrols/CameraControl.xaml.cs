using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vhr;
using WpfCap;

namespace VhR.SimpleGrblGui.Usercontrols
{
    public partial class CameraControl : UserControl
    {
        private Image centerLines = null;

        public CameraControl()
        {
            InitializeComponent();

            DeviceBox.ItemsSource = CapDevice.Devices;
            DeviceBox.DisplayMemberPath = "Name";
            DeviceBox.SelectionChanged += DeviceBox_SelectionChanged;
            DeviceBox.SelectedIndex = 0;

            ColorCombo.SelectionChanged += ColorCombo_SelectionChanged;
            ColorCombo.SelectedColor = Brushes.Red;
        }

        private void DeviceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Player.Device = (CapDevice)DeviceBox.SelectedItem;
        }

        private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawGrid();
        }

        void CapPlayer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGrid();
        }

        private void DrawGrid()
        {
            if (Player != null && centerLines != null)
            {
                int verticallines = 5;
                int horizontallines = 5;
                int circles = 3;

                CameraGrid.Children.Remove(centerLines);
                DrawingGroup gridlines = new DrawingGroup();

                double videoheigth = Player.ActualHeight == 0 ? 2 : Player.ActualHeight;
                double videowidth = Player.ActualWidth == 0 ? 2 : Player.ActualWidth;

                double videoverticalhalf = videoheigth / 2;
                double videohorizontalhalf = videowidth / 2;

                Point lowerleft = new Point(0, 0);
                Point lowerright = new Point(videowidth, 0);
                Point upperleft = new Point(0, videoheigth);
                Point upperright = new Point(videowidth, videoheigth);
                Point centre = new Point(videohorizontalhalf, videoverticalhalf);

                /*First a rectangle for the player */
                RectangleGeometry rectangle = new RectangleGeometry(new Rect(lowerleft,upperright));
                GeometryDrawing rectangledraw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = rectangle
                };
                gridlines.Children.Add(rectangledraw);
               
                /*vertical lines*/
                double verticalspace = videoheigth / (horizontallines + 1);
                for (int y=0; y<= horizontallines; y++)
                {
                    LineGeometry horizontal = new LineGeometry(new Point(0, y* verticalspace), new Point(videowidth, y * verticalspace));
                    GeometryDrawing horizontaldraw = new GeometryDrawing
                    {
                        Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                        Geometry = horizontal,
                        
                    };
                    gridlines.Children.Add(horizontaldraw);
                }

                /*horizontal lines*/
                double horizontalspace = videowidth / (verticallines + 1);
                for (int x = 0; x <= verticallines; x++)
                {
                    LineGeometry vertictal = new LineGeometry(new Point(x* horizontalspace, 0), new Point(x * horizontalspace, videoheigth));
                    GeometryDrawing verticaldraw = new GeometryDrawing
                    {
                        Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                        Geometry = vertictal
                    };
                    gridlines.Children.Add(verticaldraw);
                }

                /* circles*/
                double dradius = (videoheigth/2) / (circles);
                for (int c = 0; c <= circles; c++)
                {
                    EllipseGeometry center = new EllipseGeometry(centre, c* dradius, c * dradius);
                    GeometryDrawing centerdraw = new GeometryDrawing
                    {
                        Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                        Geometry = center
                    };
                    gridlines.Children.Add(centerdraw);
                }

                EllipseGeometry center1 = new EllipseGeometry(centre, 25,25);
                GeometryDrawing centerdraw1 = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = center1
                };
                gridlines.Children.Add(centerdraw1);

                /* diagonals */
                LineGeometry diagonal1 = new LineGeometry(upperleft, lowerright);
                GeometryDrawing diagonal1draw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = diagonal1
                };
                gridlines.Children.Add(diagonal1draw);

                LineGeometry diagonal2 = new LineGeometry(lowerleft, upperright);
                GeometryDrawing diagonal2draw = new GeometryDrawing
                {
                    Pen = new Pen(ColorCombo.SelectedColor, 0.5),
                    Geometry = diagonal2
                };
                gridlines.Children.Add(diagonal2draw);

                centerLines.Source = new DrawingImage(gridlines);
                Panel.SetZIndex(centerLines, 2);
                CameraGrid.Children.Add(centerLines);
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) //control is visible
            {
                Player.Device?.Start();
                centerLines = new Image();
                DrawGrid();
            }
            else
            {
                CameraGrid.Children.Remove(centerLines);
                centerLines = null;
                Player.Device?.Stop();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Player.Device?.Stop();
        }
    }
}
