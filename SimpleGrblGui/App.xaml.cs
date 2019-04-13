using log4net;
using System.Windows;
using System.Windows.Threading;
using Vhr;

namespace VhR.SimpleGrblGui
{
    public partial class App : Application
    {
        public static ILog Log
        {
            get
            {
                return LogManager.GetLogger(typeof(App));
            }
        }

        public static Grbl Grbl
        {
            get
            {
                return Grbl.Interface;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Info("Starting SimpleGrblGui");
            Grbl.Start();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Grbl.Stop();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.Message);
            e.Handled = true;
        }
    }
}
