using System;
using System.Windows.Input;

namespace VhR.SimpleGrblGui.Classes
{
    public class DelegatingCommand : ICommand
    {
        private readonly Action<object> action;
        private readonly Func<object,bool> canExecute;

        public DelegatingCommand(Action _action) : this((obj) => _action())
        { }
        public DelegatingCommand(Action _action, Func<object, bool> _canExecute) : this((obj) => _action(), (i) => _canExecute(i))
        { }
        public DelegatingCommand(Action<object> _action) :this(_action ,(obj)=>true)
        { }
        public DelegatingCommand(Action<object> _action, Func<object, bool> _canExecute)
        {
            action = _action;
            canExecute = _canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            action(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
