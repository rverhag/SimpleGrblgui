using Microsoft.Win32;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Vhr;
using Vhr.Gcode;
using VhR.SimpleGrblGui.Usercontrols;
using VhR.SimpleGrblGui.ViewModels;

namespace VhR.SimpleGrblGui
{
    public partial class Main : Window
    {
        private Grbl grbl;
        private CameraControl cameracontrol;
        //private Gcode gcode;
       // private Drawing drawing = new Drawing();

        public Main()
        {
            InitializeComponent();
            Title = ConfigurationManager.AppSettings["ApplicationName"].ToString();
            
            Menu_Camera.Visibility = Convert.ToBoolean(ConfigurationManager.AppSettings["Camera"])? Visibility.Visible : Visibility.Hidden;

            DataContext = new MainViewModel();
            grbl = Grbl.Interface;
        }

        private void Menu_open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.FileName = "*.nc"; 
            dlg.DefaultExt = ".nc"; // Default file extension
            dlg.Filter = "Cnc gcode (.nc)|*.nc;*.cnc"; // Filter files by extension

            if (dlg.ShowDialog() == true)
            {
                grbl.Gcode = new GcodeCollection(dlg.FileName);
                //gcode = new Gcode();
                //gcode.Show();

              //  drawing.Show();
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingswindow = new SettingsWindow();
            settingswindow.ShowDialog();
        }

        private void Menu_Camera_Click(object sender, RoutedEventArgs e)
        {
            if (cameracontrol != null)
            {
                Menu_Camera.Header = "_Camera";
                HideCamera();
            }
            else
            {
                Menu_Camera.Header = "_Drawing";
                ShowCamera();
            }
        }

        private void ShowCamera()
        {
            //if (cameracontrol == null)
            //{
            //    cameracontrol = new CameraControl();
            //    Grid.SetRow(ContentGrid, 1);
            //    ContentGrid.Children.Add(cameracontrol);
            //}
        }

        private void HideCamera()
        {
            //if (cameracontrol != null)
            //{
            //    ContentGrid.Children.Remove(cameracontrol);
            //    cameracontrol = null;
            //}
        }
    }
}
