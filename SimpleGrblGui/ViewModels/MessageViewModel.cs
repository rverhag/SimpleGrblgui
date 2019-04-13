using System.ComponentModel;
using Vhr;
using Vhr.Events;

namespace VhR.SimpleGrblGui.ViewModels
{
    public class MessageViewModel : INotifyPropertyChanged
    {
       
        public event PropertyChangedEventHandler PropertyChanged;

        public MessageViewModel()
        {
            App.Grbl.MessageReceived += Grbl_MessageReceived;
        }

        private void Grbl_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FeedbackMessage"));
        }

        public string FeedbackMessage
        {
            get { return App.Grbl.FeedbackMessage; }
        }

    }
}
