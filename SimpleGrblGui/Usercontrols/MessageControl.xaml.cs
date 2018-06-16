using System.Windows.Controls;
using VhR.SimpleGrblGui.ViewModels;

namespace VhR.SimpleGrblGui.Usercontrols
{
   
    public partial class MessageControl : UserControl
    {
        public MessageControl()
        {
            InitializeComponent();
            DataContext = new MessageViewModel();
        }
    }
}
