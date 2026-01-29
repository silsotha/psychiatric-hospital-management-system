using System;
using System.Windows;
using System.Windows.Media;

namespace PsychiatricHospitalWPF.Views.Prescriptions
{
    public partial class ExecutePrescriptionWindow : Window
    {
        public DateTime ExecutionDateTime { get; private set; }
        public string Notes { get; private set; }

        private bool isTimeUpdating = false;

        public ExecutePrescriptionWindow(string prescriptionName)
        {
            InitializeComponent();

            txtPrescriptionInfo.Text = string.Format("Назначение: {0}", prescriptionName);

            // установка текущей даты и времени по умолчанию
            dpExecutionDate.SelectedDate = DateTime.Now;
            txtExecutionTime.Text = DateTime.Now.ToString("HH:mm");

            txtExecutionTime.Focus();
        }

        // валидация времени (копируем логику из AddRecordWindow)
        private void TxtExecutionTime_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // ...копируем логику валидации времени из AddRecordWindow.xaml.cs...
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // валидация даты
            if (!dpExecutionDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату выполнения");
                dpExecutionDate.Focus();
                return;
            }

            // валидация времени
            if (string.IsNullOrWhiteSpace(txtExecutionTime.Text) ||
                txtExecutionTime.Text.Length != 5)
            {
                ShowError("Введите время в формате ЧЧ:ММ");
                txtExecutionTime.Focus();
                return;
            }

            TimeSpan executionTime;
            if (!TimeSpan.TryParse(txtExecutionTime.Text, out executionTime))
            {
                ShowError("Неверный формат времени");
                txtExecutionTime.Focus();
                return;
            }

            // формируем полную дату-время
            ExecutionDateTime = dpExecutionDate.SelectedDate.Value.Date + executionTime;

            // проверка: время не в будущем
            if (ExecutionDateTime > DateTime.Now)
            {
                ShowError("Дата и время выполнения не могут быть в будущем");
                return;
            }

            Notes = txtNotes.Text.Trim();

            this.DialogResult = true;
            this.Close();
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