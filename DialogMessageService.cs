using System.Windows;

namespace SimpleInventoryApp
{
    class DialogMessageService : IDialogMessageService
    {
        public void Show(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
