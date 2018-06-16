using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vhr.Events
{
    public class FeedRateChangedEventArgs : EventArgs
    {
        public FeedRateChangedEventArgs(int _from, int _to)
        {
            From = _from;
            To = _to;
        }
        public int From { get; private set; }
        public int To { get; private set; }
    }
}
