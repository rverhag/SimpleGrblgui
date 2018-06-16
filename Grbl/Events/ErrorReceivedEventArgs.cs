using System;

namespace Vhr.Events
{
    public class ErrorReceivedEventArgs : EventArgs
    {
        public ErrorReceivedEventArgs(string _error)
        {
            Error = _error;
        }

        public string Error { get; private set; }
    }
}
