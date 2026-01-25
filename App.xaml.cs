using System.Windows;
using PsychiatricHospitalWPF.Views.Auth;

namespace PsychiatricHospitalWPF
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Сначала показываем окно входа!
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}