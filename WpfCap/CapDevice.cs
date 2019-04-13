using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfCap
{
    public class CapDevice : DependencyObject, IDisposable
	{
		#region Variables

		private ManualResetEvent _stopSignal;
		private Task _captureTask;
		private IFilterGraph2 _graph = null;
		private ISampleGrabber _grabber = null;
		private IBaseFilter _sourceObject = null;
		private IBaseFilter _grabberObject = null;
		private IMediaControl _control = null;
		private CapGrabber _capGrabber = null;
		private IntPtr _map = IntPtr.Zero;
		private IntPtr _section = IntPtr.Zero;

		private Stopwatch _timer = Stopwatch.StartNew();
		private double _frames;
		private string _monikerString = string.Empty;
		private readonly int _desiredWidth;
		private readonly int _desiredHeight;
		#endregion

		#region Constructor & destructor
		/// <summary>
		/// Initializes the default capture device
		/// </summary>
		/// <param name="desiredHeight">the desired height</param>
		/// <param name="desiredWidth">the desired width</param>
		public CapDevice(int desiredWidth, int desiredHeight)
			: this(DeviceMonikers[0].MonikerString, desiredWidth = 0, desiredHeight = 0)
		{ }

		/// <summary>
		/// Initializes a specific capture device
		/// </summary>
		/// <param name="moniker">Moniker string that represents a specific device</param>
		/// <param name="desiredHeight">the desired height</param>
		/// <param name="desiredWidth">the desired width</param>
		public CapDevice(string moniker, int desiredWidth = 0, int desiredHeight = 0)
		{
			// Store moniker (since dependency properties are not thread-safe, store it locally as well)
			_monikerString = moniker;
			MonikerString = moniker;

			_desiredWidth = desiredWidth;
			_desiredHeight = desiredHeight;

			// Find the name
			foreach (FilterInfo filterInfo in DeviceMonikers)
			{
				if (filterInfo.MonikerString == moniker)
				{
					Name = filterInfo.Name;
					break;
				}
			}
		}


		#endregion

		#region Events
		/// <summary>
		/// Event that is invoked when a new bitmap is ready
		/// </summary>
		public event EventHandler NewBitmapReady;

		/// <summary>
		/// Event notifying of new frame being available.
		/// </summary>
		public event EventHandler FrameAvailable;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the device monikers
		/// </summary>
		public static FilterInfo[] DeviceMonikers
		{
			get
			{
				List<FilterInfo> filters = new List<FilterInfo>();
				IMoniker[] ms = new IMoniker[1];
				ICreateDevEnum enumD = Activator.CreateInstance(Type.GetTypeFromCLSID(SystemDeviceEnum)) as ICreateDevEnum;
                Guid g = VideoInputDevice;
                if (enumD.CreateClassEnumerator(ref g, out IEnumMoniker moniker, 0) == 0)
				{
					while (true)
					{
						int r = moniker.Next(1, ms, IntPtr.Zero);
						if (r != 0 || ms[0] == null)
							break;
						filters.Add(new FilterInfo(ms[0]));
						Marshal.ReleaseComObject(ms[0]);
						ms[0] = null;
					}
				}

				return filters.ToArray();
			}
		}

		/// <summary>
		/// Gets the available devices
		/// </summary>
		public static CapDevice[] Devices
		{
			get
			{
				// Declare variables
				List<CapDevice> devices = new List<CapDevice>();

				// Loop all monikers
				foreach (FilterInfo moniker in DeviceMonikers)
				{
					devices.Add(new CapDevice(moniker.MonikerString));
				}

				// Return result
				return devices.ToArray();
			}
		}

		/// <summary>
		/// Wrapper for the BitmapSource dependency property
		/// </summary>
		public InteropBitmap BitmapSource
		{
			get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
			private set { SetValue(BitmapSourcePropertyKey, value); }
		}

		private static readonly DependencyPropertyKey BitmapSourcePropertyKey =
			DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(CapDevice), new UIPropertyMetadata(default(InteropBitmap)));

		public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;

		/// <summary>
		/// Wrapper for the Name dependency property
		/// </summary>
		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty NameProperty =
			DependencyProperty.Register("Name", typeof(string), typeof(CapDevice), new UIPropertyMetadata(""));

		/// <summary>
		/// Wrapper for the MonikerString dependency property
		/// </summary>
		public string MonikerString
		{
			get { return (string)GetValue(MonikerStringProperty); }
			set { SetValue(MonikerStringProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MonikerString.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MonikerStringProperty =
			DependencyProperty.Register("MonikerString", typeof(string), typeof(CapDevice), new UIPropertyMetadata(""));

		/// <summary>
		/// Wrapper for the Framerate dependency property
		/// </summary>
		public float Framerate
		{
			get { return (float)GetValue(FramerateProperty); }
			set { SetValue(FramerateProperty, value); }
		}

		public static readonly DependencyProperty FramerateProperty =
			DependencyProperty.Register("Framerate", typeof(float), typeof(CapDevice), new UIPropertyMetadata(default(float)));

		#endregion

		#region Methods
		/// <summary>
		/// Invoked when a new frame arrived
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">EventArgs</param>
		private void capGrabber_NewFrameArrived(object sender, EventArgs e)
		{
			// Make sure to be thread safe
			if (Dispatcher != null)
			{
				this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, (SendOrPostCallback)delegate
				{
					if (BitmapSource != null)
					{
						BitmapSource.Invalidate();
						UpdateFramerate();

                        FrameAvailable?.Invoke(this, null);
                    }
				}, null);
			}
		}

		/// <summary>
		/// Invoked when a property of the CapGrabber object has changed
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">PropertyChangedEventArgs</param>
		private void capGrabber_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.DataBind, (SendOrPostCallback)delegate
			{
				try
				{
					if ((_capGrabber.Width != default(int)) && (_capGrabber.Height != default(int)))
					{
						// Get the pixel count
						uint pcount = (uint)(_capGrabber.Width * _capGrabber.Height * PixelFormats.Bgr32.BitsPerPixel / 8);

						// Create a file mapping
						_section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, pcount, null);
						_map = MapViewOfFile(_section, 0xF001F, 0, 0, pcount);

						// Get the bitmap
						BitmapSource = Imaging.CreateBitmapSourceFromMemorySection(_section, _capGrabber.Width,
							_capGrabber.Height, PixelFormats.Bgr32, _capGrabber.Width * PixelFormats.Bgr32.BitsPerPixel / 8, 0) as InteropBitmap;
						_capGrabber.Map = _map;

                        // Invoke event
                        NewBitmapReady?.Invoke(this, null);
                    }
				}
				catch (Exception ex)
				{
					// Trace
					Trace.TraceError(ex.Message);
				}
			}, null);
		}

		private void SelectWebcamResolution(IPin sourcePin)
		{
			var cfg = sourcePin as IAMStreamConfig;

			int capabilitiesCount = 0;
			int capabilitiesResultStructureSize = 0;
			var result = cfg.GetNumberOfCapabilities(out capabilitiesCount, out capabilitiesResultStructureSize);

			if (result == 0)
			{
				var caps = new VideoStreamConfigCaps();
				var gcHandle = GCHandle.Alloc(caps, GCHandleType.Pinned);

				try
				{
					for (int i = 0; i != capabilitiesCount; ++i)
					{
						AMMediaType capabilityInfo = null;
						result = cfg.GetStreamCaps(i, out capabilityInfo, gcHandle.AddrOfPinnedObject());
						using (capabilityInfo)
						{
							var infoHeader = (VideoInfoHeader)Marshal.PtrToStructure(capabilityInfo.FormatPtr, typeof(VideoInfoHeader));

							if (infoHeader.BmiHeader.Width == _desiredWidth &&
								infoHeader.BmiHeader.Height == _desiredHeight &&
								infoHeader.BmiHeader.BitCount != 0)
							{
								result = cfg.SetFormat(capabilityInfo);
								break;
							}
						}
					}
				}
				finally
				{ gcHandle.Free(); }
			}
		}

		/// <summary>
		/// Updates the framerate
		/// </summary>
		private void UpdateFramerate()
		{
			// Increase the frames
			_frames++;

			// Check the timer
			if (_timer.ElapsedMilliseconds < 1000) return;
			// Set the framerate
			Framerate = (float)Math.Round(_frames * 1000 / _timer.ElapsedMilliseconds);

			// Reset the timer again so we can count the framerate again
			_timer.Reset();
			_timer.Start();
			_frames = 0;
		}

		/// <summary>;
		/// Starts grabbing images from the capture device
		/// </summary>
		public virtual void Start()
		{
			if (_captureTask != null)
			{
				Stop();
			}

			_captureTask = new Task(() =>
			{
				// Create new grabber
				_capGrabber = new CapGrabber();
				_capGrabber.PropertyChanged += capGrabber_PropertyChanged;
				_capGrabber.NewFrameArrived += capGrabber_NewFrameArrived;
				_stopSignal = new ManualResetEvent(false);

				_graph = Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph)) as IFilterGraph2;
				_sourceObject = FilterInfo.CreateFilter(_monikerString);

				var outputPin = _sourceObject.GetPin(PinCategory.Capture, 0);
				SelectWebcamResolution(outputPin);

				_grabber = Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber)) as ISampleGrabber;
				_grabberObject = _grabber as IBaseFilter;

				if (_graph == null)
				{ return; };

				_graph.AddFilter(_sourceObject, "source");
				_graph.AddFilter(_grabberObject, "grabber");
				using (var mediaType = new AMMediaType())
				{
					mediaType.MajorType = MediaTypes.Video;
					mediaType.SubType = MediaSubTypes.RGB32;
					if (_grabber != null)
					{
						_grabber.SetMediaType(mediaType);
						
						
						var inputPin = _grabberObject.GetPin(PinDirection.Input, 0);
						if (_graph.Connect(outputPin, inputPin) >= 0)
						{
							if (_grabber.GetConnectedMediaType(mediaType) == 0)
							{
								var header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
								_capGrabber.Width = header.BmiHeader.Width;
								_capGrabber.Height = header.BmiHeader.Height;
							}
						}
						_graph.Render(_grabberObject.GetPin(PinDirection.Output, 0));
						_grabber.SetBufferSamples(false);
						_grabber.SetOneShot(false);
						_grabber.SetCallback(_capGrabber, 1);
					}

					// Get the video window
					var wnd = (IVideoWindow)_graph;
					wnd.put_AutoShow(false);

					// Create the control and run
					_control = (IMediaControl)_graph;

					_control.Run();

					// Wait for the stop signal
					_stopSignal.WaitOne();
					Cleanup();
				}
			});
			_captureTask.Start();
		}

		private void Cleanup()
		{
			if (_control != null)
			{
				// Stop when ready
				_control.StopWhenReady();
			}

			_graph = null;
			_sourceObject = null;
			_grabberObject = null;
			_grabber = null;
			_capGrabber = null;
			_control = null;
		}

		/// <summary>
		/// Stops grabbing images from the capture device
		/// </summary>
		public virtual void Stop()
		{
            if (_stopSignal != null)
            { _stopSignal.Set(); }
		}
		#endregion

		#region Win32
		private static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

		private static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

		public static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

		public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

		public static readonly Guid Pin = new Guid(0x9b00f101, 0x1567, 0x11d1, 0xb3, 0xf1, 0x00, 0xaa, 0x00, 0x37, 0x61, 0xc5);

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

		[ComVisible(false)]
		static public class PinCategory
		{
			public static readonly Guid Capture = new Guid(0xfb6c4281, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid Preview = new Guid(0xfb6c4282, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid AnalogVideoIn = new Guid(0xfb6c4283, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid VBI = new Guid(0xfb6c4284, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid VideoPort = new Guid(0xfb6c4285, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid NABTS = new Guid(0xfb6c4286, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid EDS = new Guid(0xfb6c4287, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid TeleText = new Guid(0xfb6c4288, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid CC = new Guid(0xfb6c4289, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid Still = new Guid(0xfb6c428a, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid TimeCode = new Guid(0xfb6c428b, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);

			public static readonly Guid VideoPortVBI = new Guid(0xfb6c428c, 0x0353, 0x11d1, 0x90, 0x5f, 0x00, 0x00, 0xc0, 0xcc, 0x16, 0xba);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Stop();
		}

		#endregion
	}
}