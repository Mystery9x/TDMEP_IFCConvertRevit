using System;
using System.Windows.Input;

namespace TepscoIFCToRevit.UI
{
    /// <summary>
    /// Implementation of ICommand interface that relays
    /// an action on UI to a logic implementation in view model
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _targetExecuteMethod;
        private readonly Func<T, bool> _targetCanExecuteMethod;

        public RelayCommand(Action<T> executeMethod)
        {
            _targetExecuteMethod = executeMethod;
        }

        public RelayCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            _targetExecuteMethod = executeMethod;
            _targetCanExecuteMethod = canExecuteMethod;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (_targetCanExecuteMethod != null)
            {
                T param = (T)parameter;
                return _targetCanExecuteMethod(param);
            }
            return _targetExecuteMethod != null;
        }

        public void Execute(object parameter)
        {
            if (_targetExecuteMethod != null)
            {
                T param = (T)parameter;
                _targetExecuteMethod(param);
            }
        }
    }
}