using System.Windows.Input;

namespace SimpleInventoryApp
{
    public class DelegateCmd : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private readonly Action<object> execute; 
        private readonly Predicate<object> canExecute;

        public DelegateCmd(Action<object> execute, Predicate<object> canExecute=null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute?.Invoke(parameter);
        }
    }
}
