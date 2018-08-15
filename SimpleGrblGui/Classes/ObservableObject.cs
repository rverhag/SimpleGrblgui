using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// This class is a copy of the class in the examples-section of HelixToolkit.Wpf.SharpDX.
/// So full credit goes to the original developers 
/// </summary>
/// 
namespace VhR.SimpleGrblGui.Classes
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string info = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        protected bool SetValue<T>(ref T backingField, T value, [CallerMemberName]string propertyName = "")
        {
            if (Equals(backingField, value))
            {
                return false;
            }

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
