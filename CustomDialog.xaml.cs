using System.Windows;
using System.Windows.Input;

namespace obhod
{
    public partial class CustomDialog : Window
    {
        public CustomDialog(string title, string message, bool isYesNo = false)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtMessage.Text = message;
            
            if (isYesNo)
            {
                BtnNo.Visibility = Visibility.Visible;
                BtnYes.Content = "Да";
            }
            else
            {
                BtnYes.Content = "ОК";
            }
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            try { this.DragMove(); } catch { }
        }

        public static bool ShowDialog(string title, string message, bool isYesNo = false)
        {
            bool? result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new CustomDialog(title, message, isYesNo);
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    dialog.Owner = Application.Current.MainWindow;
                }
                result = dialog.ShowDialog();
            });
            return result == true;
        }
    }
}
