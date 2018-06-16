using System;

namespace Vhr.Events
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string _message)
        {
            Message = _message;
        }

        public string Message { get; private set; }
    }
}
