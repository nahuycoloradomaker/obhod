using System.Windows;
using System.Threading.Tasks;

namespace obhod
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public static async Task ShowAndLoad()
        {
            var splash = new SplashWindow();
            splash.Show();
            
            await Task.Delay(3000);
            
            splash.Close();
        }
    }
}
