using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfCap
{
    public class CapPlayer : Image,IDisposable
    {
        private CapDevice _device = null;

        public float Framerate
        {
            get { return (float)GetValue(FramerateProperty); }
            set { SetValue(FramerateProperty, value); }
        }
        public static readonly DependencyProperty FramerateProperty = DependencyProperty.Register("Framerate", typeof(float), typeof(CapPlayer), new UIPropertyMetadata(default(float)));

        public CapPlayer()
        {
            if (_device == null)
            {
                _device = new CapDevice();
                _device.OnNewBitmapReady += new EventHandler(_device_OnNewBitmapReady);
            }

            Application.Current.Exit += new ExitEventHandler(Current_Exit);
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            this.Dispose();
        }

        void _device_OnNewBitmapReady(object sender, EventArgs e)
        {
            Binding b = new Binding();
            b.Source = _device;
            b.Path = new PropertyPath(CapDevice.FramerateProperty);
            this.SetBinding(CapPlayer.FramerateProperty, b);

            this.Source = _device.BitmapSource;
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (_device != null)
            {
                _device.Stop();
                _device.OnNewBitmapReady -= _device_OnNewBitmapReady;
                _device.Dispose();
                _device = null;
            }
        }

        #endregion
    }
}
