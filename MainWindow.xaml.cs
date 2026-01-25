using PsychiatricHospitalWPF.Utils;
using PsychiatricHospitalWPF.Views.Auth;
using PsychiatricHospitalWPF.Views.MedicalRecords;
using PsychiatricHospitalWPF.Views.Patients;
using System.Windows;

namespace PsychiatricHospitalWPF
{
    public partial class MainWindow : Window
    {
        // Хранилище для единственного экземпляра окна списка пациентов
        private PatientsListWindow patientsListWindow = null;

        public MainWindow()
        {
            InitializeComponent();

            lblUserInfo.Text = string.Format("Пользователь: {0} ({1})",
                UserSession.CurrentUser.FullName,
                UserSession.CurrentUser.Role);
        }

        private void BtnPatients_Click(object sender, RoutedEventArgs e)
        {
            OpenPatientsListWindow();
        }

        /// <summary>
        /// Открытие окна списка пациентов (единственный экземпляр)
        /// </summary>
        private void OpenPatientsListWindow()
        {
            // Проверяем, существует ли уже окно
            if (patientsListWindow != null)
            {
                // Окно существует - активируем его
                if (patientsListWindow.IsLoaded)
                {
                    // Если окно свернуто - разворачиваем
                    if (patientsListWindow.WindowState == WindowState.Minimized)
                    {
                        patientsListWindow.WindowState = WindowState.Normal;
                    }

                    // Активируем и переносим на передний план
                    patientsListWindow.Activate();
                    patientsListWindow.Focus();
                    return;
                }
            }

            // Создаем новое окно
            patientsListWindow = new PatientsListWindow();

            // Подписываемся на событие закрытия, чтобы обнулить ссылку
            patientsListWindow.Closed += (s, args) => patientsListWindow = null;

            patientsListWindow.Show();
        }

        private void BtnWards_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Открыть окно управления палатами
            MessageBox.Show(
                "Функционал управления палатами в разработке",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Открыть окно отчетов
            MessageBox.Show(
                "Функционал отчетов в разработке",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Закрываем все дочерние окна
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != this && window.IsLoaded)
                    {
                        window.Close();
                    }
                }

                UserSession.Logout();

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}