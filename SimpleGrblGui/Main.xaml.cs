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
        private CameraControl cameracontrol;

        public Main()
        {
            InitializeComponent();

            Title = ConfigurationManager.AppSettings["ApplicationName"].ToString();
            Menu_Camera.Visibility = Convert.ToBoolean(ConfigurationManager.AppSettings["Camera"])? Visibility.Visible : Visibility.Hidden;
            DataContext = new MainViewModel();
        }

        private void Menu_open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Cnc gcode (.nc)|*.nc;*.cnc;*.ngc;*.gcode" 
            };

            if (dlg.ShowDialog() == true)
            {
                App.Grbl.Gcode = new GcodeCollection(dlg.FileName);
            }
        }

        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingswindow = new SettingsWindow();
            settingswindow.ShowDialog();
        }

        private void Menu_Camera_Click(object sender, RoutedEventArgs e)
        {
            if (Camera.Visibility == Visibility.Visible)
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
            Drawing.Visibility = Visibility.Hidden;
            Camera.Visibility = Visibility.Visible;
        }

        private void HideCamera()
        {
            Camera.Visibility = Visibility.Hidden;
            Drawing.Visibility = Visibility.Visible;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.Grbl.Reset();
        }
    }
}
