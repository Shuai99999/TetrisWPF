using System.Threading.Tasks;
using System.Windows;

namespace TetrisWPF
{
    public partial class InputDialog : Window
    {
        public string? Result { get; private set; }

        public InputDialog()
        {
            InitializeComponent();
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }

        public static async Task<string?> ShowAsync(Window owner)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new InputDialog
                {
                    Owner = owner
                };
                if (dialog.ShowDialog() == true)
                {
                    return dialog.Result;
                }
                return null;
            });
        }
    }
}

