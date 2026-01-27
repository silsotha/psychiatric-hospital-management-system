using PsychiatricHospitalWPF.Utils;
using PsychiatricHospitalWPF.Views.Auth;
using PsychiatricHospitalWPF.Views.Patients;
using PsychiatricHospitalWPF.Views.Reports;
using PsychiatricHospitalWPF.Views.Wards;
using System;
using System.Windows;

namespace PsychiatricHospitalWPF
{
    public partial class MainWindow : Window
    {
        // ссылки на открытые окна для предотвращения дублирования
        private Window patientsWindow = null;
        private WardManagementWindow wardManagementWindow = null;
        private ReportsWindow reportsWindow = null;

        public MainWindow()
        {
            InitializeComponent();

            // отображаем информацию о текущем пользователе
            if (UserSession.IsAuthenticated)
            {
                lblUserInfo.Text = string.Format("Пользователь: {0} ({1})",
                    UserSession.CurrentUser.FullName,
                    GetRoleDisplayName(UserSession.CurrentUser.Role));
            }
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role?.ToLower())
            {
                case "doctor":
                    return "Врач";
                case "nurse":
                    return "Медсестра";
                case "admin":
                    return "Администратор";
                default:
                    return role;
            }
        }

        private void BtnPatients_Click(object sender, RoutedEventArgs e)
        {
            // если окно уже открыто - активируем его
            if (patientsWindow != null && patientsWindow.IsLoaded)
            {
                patientsWindow.Activate();
                patientsWindow.WindowState = WindowState.Normal;
                return;
            }

            // создаём новое окно
            patientsWindow = new PatientsListWindow();

            // подписываемся на событие закрытия для очистки ссылки
            patientsWindow.Closed += (s, args) => patientsWindow = null;

            patientsWindow.Show();
        }

        private void BtnWards_Click(object sender, RoutedEventArgs e)
        {
            // если окно уже открыто - активируем его
            if (wardManagementWindow != null && wardManagementWindow.IsLoaded)
            {
                wardManagementWindow.Activate();
                wardManagementWindow.WindowState = WindowState.Normal;
                return;
            }

            // создаём новое окно
            wardManagementWindow = new WardManagementWindow();

            // подписываемся на событие закрытия для очистки ссылки
            wardManagementWindow.Closed += (s, args) => wardManagementWindow = null;

            wardManagementWindow.Show();
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            // если окно уже открыто - активируем его
            if (reportsWindow != null && reportsWindow.IsLoaded)
            {
                reportsWindow.Activate();
                reportsWindow.WindowState = WindowState.Normal;
                return;
            }

            // создаём новое окно
            reportsWindow = new ReportsWindow();

            // подписываемся на событие закрытия для очистки ссылки
            reportsWindow.Closed += (s, args) => reportsWindow = null;

            reportsWindow.Show();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // очищаем сессию
                UserSession.Logout();

                // закрываем все дочерние окна
                CloseAllChildWindows();

                // открываем окно авторизации
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                // закрываем главное окно
                this.Close();
            }
        }

        private void CloseAllChildWindows()
        {
            // закрываем все открытые окна
            if (patientsWindow != null && patientsWindow.IsLoaded)
            {
                patientsWindow.Close();
                patientsWindow = null;
            }

            if (wardManagementWindow != null && wardManagementWindow.IsLoaded)
            {
                wardManagementWindow.Close();
                wardManagementWindow = null;
            }

            if (reportsWindow != null && reportsWindow.IsLoaded)
            {
                reportsWindow.Close();
                reportsWindow = null;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // при закрытии главного окна закрываем все дочерние
            CloseAllChildWindows();
            base.OnClosing(e);
        }
    }
}