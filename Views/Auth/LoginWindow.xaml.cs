using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using System;
using System.Windows;
using System.Windows.Input;

namespace PsychiatricHospitalWPF.Views.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService authService;

        public LoginWindow()
        {
            InitializeComponent();
            authService = new AuthService();

            // фокус на поле имени пользователя
            Loaded += (s, e) => txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformLogin();
            }
        }

        private void PerformLogin()
        {
            HideError();

            // валидация
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                ShowError("Введите имя пользователя");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                ShowError("Введите пароль");
                txtPassword.Focus();
                return;
            }

            try
            {
                // блокируем UI
                btnLogin.IsEnabled = false;
                btnLogin.Content = "Проверка...";

                var user = authService.Authenticate(
                    txtUsername.Text.Trim(),
                    txtPassword.Password);

                if (user != null)
                {
                    UserSession.Login(user);

                    authService.AddAuditLog(
                        user.UserId, "LOGIN", "User", user.UserId);

                    // открываем главное окно
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // закрываем окно входа
                    this.Close();
                }
                else
                {
                    ShowError("Неверное имя пользователя или пароль");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Войти";
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            lblError.Visibility = Visibility.Collapsed;
        }
    }
}