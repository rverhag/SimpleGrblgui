using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfCap
{
    internal class CapDevice : DependencyObject, IDisposable
    {
        private bool wantstop = false;

        private Thread worker = null;
        private IGraphBuilder graph = null;
        private ISampleGrabber grabber = null;
        private IBaseFilter sourceObject = null, grabberObject = null;
        private IMediaControl control = null;
        private IVideoWindow wnd = null;

        private CapGrabber capGrabber = null;
        
        private static string deviceMoniker = null;
        private IntPtr section;
        private Stopwatch stopwatch = null;

        

        private static readonly DependencyPropertyKey BitmapSourcePropertyKey = DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CapDevice), new UIPropertyMetadata(default(InteropBitmap)));
        public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;
        public static readonly DependencyProperty FramerateProperty = DependencyProperty.Register("Framerate", typeof(float), typeof(CapDevice), new UIPropertyMetadata(default(float)));

        private double frames;

        public InteropBitmap BitmapSource
        {
            get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
            private set { SetValue(BitmapSourcePropertyKey, value); }
        }

        public float Framerate
        {
            get { return (float)GetValue(FramerateProperty); }
            set { SetValue(FramerateProperty, value); }
        }

        public CapDevice()
        {
            if (DeviceMonikers.Count() <= 0)
            {
                throw new NotSupportedException("You should connect DirectDraw source first");
            }
            else
            {
                deviceMoniker = DeviceMonikers[0].MonikerString;
                Start();
            }
        }

        private void capGrabber_NewFrameArrived(object sender, EventArgs e)
        {
            if (this.Dispatcher != null)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, (SendOrPostCallback)delegate
                {
                   // Debug.WriteLine(string.Concat("capGrabber_NewFrameArrived"));

                    if ((BitmapSource != null))
                    {
                        BitmapSource.Invalidate();
                    }
                }, null);
            }
        }

        private void capGrabber_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.DataBind, (SendOrPostCallback)delegate
            {
                if (capGrabber.Width != default(int) && capGrabber.Height != default(int))
                {
                    uint pcount = (uint)(capGrabber.Width * capGrabber.Height * PixelFormats.Bgr32.BitsPerPixel / 8);
                    section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, pcount, null);

                    capGrabber.Map = IntPtr.Zero;
                    capGrabber.Map = MapViewOfFile(section, 0xF001F, 0, 0, pcount);
                    
                    BitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromMemorySection(section, capGrabber.Width, capGrabber.Height, PixelFormats.Bgr32, capGrabber.Width * PixelFormats.Bgr32.BitsPerPixel / 8, 0) as InteropBitmap;
                    section = IntPtr.Zero;

                    OnNewBitmapReady?.Invoke(this, null);
                }
            }, null);
        }

        private void Start()
        {
            if (worker == null)
            {
                capGrabber = new CapGrabber();
                capGrabber.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(capGrabber_PropertyChanged);
                capGrabber.NewFrameArrived += new EventHandler(capGrabber_NewFrameArrived);

                wantstop = false;
                worker = new Thread(RunWorker);
                worker.Start();
            }
           
        }


        public void Stop()
        {
            wantstop = true;
        }

      

        public static FilterInfo[] DeviceMonikers
        {
            get
            {
                List<FilterInfo> filters = new List<FilterInfo>();
                IMoniker[] ms = new IMoniker[1];
                ICreateDevEnum enumD = Activator.CreateInstance(Type.GetTypeFromCLSID(SystemDeviceEnum)) as ICreateDevEnum;
                IEnumMoniker moniker;
                Guid g = VideoInputDevice;
                if (enumD.CreateClassEnumerator(ref g, out moniker, 0) == 0)
                {
                    while (true)
                    {
                        int r = moniker.Next(1, ms, IntPtr.Zero);
                        if (r != 0 || ms[0] == null)
                        {
                            break;
                        }
                        filters.Add(new FilterInfo(ms[0]));
                        Marshal.ReleaseComObject(ms[0]);
                        ms[0] = null;
                    }
                }

                return filters.ToArray();
            }
        }

        void RunWorker()
        {
            try
            {
                graph = Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph)) as IGraphBuilder;

                sourceObject = FilterInfo.CreateFilter(deviceMoniker);

                grabber = Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber)) as ISampleGrabber;
                grabberObject = grabber as IBaseFilter;

                graph.AddFilter(sourceObject, "source");
                graph.AddFilter(grabberObject, "grabber");

                using (AMMediaType mediaType = new AMMediaType())
                {
                    mediaType.MajorType = MediaTypes.Video;
                    mediaType.SubType = MediaSubTypes.RGB32;
                    grabber.SetMediaType(mediaType);

                    if (graph.Connect(sourceObject.GetPin(PinDirection.Output, 0), grabberObject.GetPin(PinDirection.Input, 0)) >= 0)
                    {
                        if (grabber.GetConnectedMediaType(mediaType) == 0)
                        {
                            VideoInfoHeader header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                            //int bitrate = header.BitRate;
                            capGrabber.Width = header.BmiHeader.Width;
                            capGrabber.Height = header.BmiHeader.Height;
                        }
                    }
                    graph.Render(grabberObject.GetPin(PinDirection.Output, 0));
                    grabber.SetBufferSamples(true);
                    grabber.SetOneShot(true);
                    grabber.SetCallback(capGrabber, 1);

                    wnd = (IVideoWindow)graph;
                    wnd.put_AutoShow(false);

                    control = (IMediaControl)graph;
                    control.Run();
                  
                    while (!wantstop)
                    {
                        Thread.Sleep(100);
                    }

                    control.StopWhenReady();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                if (graph != null && sourceObject!=null)
                {
                    graph.Disconnect(sourceObject.GetPin(PinDirection.Output, 0));
                    graph = null;
                }

                if (wnd!=null)
                {
                    wnd = null;
                }

                if (capGrabber != null)
                {
                    capGrabber.PropertyChanged -= capGrabber_PropertyChanged;
                    capGrabber.NewFrameArrived -= capGrabber_NewFrameArrived;
                    capGrabber = null;
                }

                sourceObject = null;
                grabberObject = null;
                grabber = null;
                control = null;
                worker = null;
            }
        }

        static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);
        static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);
        public static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);
        public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        [ComVisible(false)]
        internal class MediaTypes
        {
            public static readonly Guid Video = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Interleaved = new Guid(0x73766169, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Audio = new Guid(0x73647561, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Text = new Guid(0x73747874, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Stream = new Guid(0xE436EB83, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);
        }

        [ComVisible(false)]
        internal class MediaSubTypes
        {
            public static readonly Guid YUYV = new Guid(0x56595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid IYUV = new Guid(0x56555949, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid DVSD = new Guid(0x44535644, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid RGB1 = new Guid(0xE436EB78, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB4 = new Guid(0xE436EB79, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB8 = new Guid(0xE436EB7A, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB565 = new Guid(0xE436EB7B, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB555 = new Guid(0xE436EB7C, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB24 = new Guid(0xE436Eb7D, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB32 = new Guid(0xE436EB7E, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Avi = new Guid(0xE436EB88, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Asf = new Guid(0x3DB80F90, 0x9412, 0x11D1, 0xAD, 0xED, 0x00, 0x00, 0xF8, 0x75, 0x4B, 0x99);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        public event EventHandler OnNewBitmapReady;


        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
