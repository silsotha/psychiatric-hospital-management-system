using System;
using System.Windows;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Views.Patients
{
    public partial class DischargePatientWindow : Window
    {
        private readonly PatientService patientService;
        private readonly int patientId;
        private readonly string patientName;

        public DischargePatientWindow(int patientId, string patientName, string currentDiagnosis)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;

            patientService = new PatientService();

            // проверка прав доступа
            if (!UserSession.CanEditMedicalRecords())
            {
                MessageBox.Show(
                    "Выписку пациента может оформить только врач!",
                    "Недостаточно прав",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                this.DialogResult = false;
                this.Close();
                return;
            }

            // настройка интерфейса
            txtPatientInfo.Text = string.Format("Пациент: {0}", patientName);
            dpDischargeDate.SelectedDate = DateTime.Now;
            dpDischargeDate.DisplayDateEnd = DateTime.Now;

            // предзаполняем заключительный диагноз текущим
            if (!string.IsNullOrEmpty(currentDiagnosis))
            {
                txtFinalDiagnosis.Text = currentDiagnosis;
            }
        }

        private void BtnDischarge_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // валидация
            if (!dpDischargeDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату выписки");
                return;
            }

            if (dpDischargeDate.SelectedDate.Value > DateTime.Now)
            {
                ShowError("Дата выписки не может быть в будущем");
                return;
            }

            if (cmbDischargeReason.SelectedItem == null)
            {
                ShowError("Выберите причину выписки");
                cmbDischargeReason.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFinalDiagnosis.Text))
            {
                ShowError("Введите заключительный диагноз");
                txtFinalDiagnosis.Focus();
                return;
            }

            if (txtFinalDiagnosis.Text.Trim().Length < 10)
            {
                ShowError("Заключительный диагноз слишком короткий (минимум 10 символов)");
                txtFinalDiagnosis.Focus();
                return;
            }

            // подтверждение
            var reason = (cmbDischargeReason.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            var result = MessageBox.Show(
                string.Format(
                    "Вы действительно хотите выписать пациента?\n\n" +
                    "Пациент: {0}\n" +
                    "Дата выписки: {1:dd.MM.yyyy}\n" +
                    "Причина: {2}\n\n" +
                    "Это действие нельзя отменить!",
                    patientName,
                    dpDischargeDate.SelectedDate.Value,
                    reason),
                "Подтверждение выписки",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // выписываем пациента
                patientService.DischargePatient(
                    patientId,
                    dpDischargeDate.SelectedDate.Value,
                    reason,
                    txtFinalDiagnosis.Text.Trim());

                MessageBox.Show(
                    string.Format(
                        "Пациент успешно выписан!\n\n" +
                        "Дата выписки: {0:dd.MM.yyyy}\n" +
                        "Причина: {1}",
                        dpDischargeDate.SelectedDate.Value,
                        reason),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowError(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                ShowError(string.Format("Ошибка: {0}", ex.Message));
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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