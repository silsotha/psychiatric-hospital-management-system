using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;

namespace PsychiatricHospitalWPF.Views.Prescriptions
{
    public partial class CreatePrescriptionWindow : Window
    {
        private readonly PrescriptionService prescriptionService;
        private readonly int patientId;
        private readonly string patientName;

        public CreatePrescriptionWindow(int patientId, string patientName)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;

            prescriptionService = new PrescriptionService();

            txtPatientName.Text = string.Format("Пациент: {0}", patientName);

            // дата начала = сегодня (по умолчанию)
            dpStartDate.SelectedDate = DateTime.Now;
            dpStartDate.DisplayDateStart = DateTime.Now;
        }

        private void CmbPrescriptionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelDosage == null) return;

            var selectedItem = cmbPrescriptionType.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string type = selectedItem.Tag.ToString();

                // дозировка обязательна только для медикаментов
                panelDosage.Visibility = type == "Медикамент"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void TxtDuration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // иолько цифры
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            try
            {
                // валидация типа назначения
                var selectedType = cmbPrescriptionType.SelectedItem as ComboBoxItem;
                if (selectedType == null)
                {
                    ShowError("Выберите тип назначения");
                    cmbPrescriptionType.Focus();
                    return;
                }

                string prescriptionType = selectedType.Tag.ToString();

                // валидация названия
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    ShowError("Введите название препарата/процедуры");
                    txtName.Focus();
                    return;
                }

                // валидация дозировки для медикаментов
                string dosage = null;
                if (prescriptionType == "Медикамент")
                {
                    if (string.IsNullOrWhiteSpace(txtDosage.Text))
                    {
                        ShowError("Введите дозировку для медикамента");
                        txtDosage.Focus();
                        return;
                    }
                    dosage = txtDosage.Text.Trim();
                }

                // валидация периодичности
                var selectedFreq = cmbFrequency.SelectedItem as ComboBoxItem;
                if (selectedFreq == null)
                {
                    ShowError("Выберите периодичность приема");
                    cmbFrequency.Focus();
                    return;
                }

                // валидация длительности
                if (string.IsNullOrWhiteSpace(txtDuration.Text))
                {
                    ShowError("Введите длительность курса");
                    txtDuration.Focus();
                    return;
                }

                if (!int.TryParse(txtDuration.Text.Trim(), out int duration) || duration <= 0)
                {
                    ShowError("Длительность должна быть положительным числом");
                    txtDuration.Focus();
                    return;
                }

                if (duration > 365)
                {
                    ShowError("Длительность не может превышать 365 дней");
                    txtDuration.Focus();
                    return;
                }

                // валидация даты начала
                if (!dpStartDate.SelectedDate.HasValue)
                {
                    ShowError("Выберите дату начала");
                    dpStartDate.Focus();
                    return;
                }

                // создаем объект назначения
                var prescription = new Prescription
                {
                    PatientId = patientId,
                    PrescriptionType = prescriptionType,
                    Name = txtName.Text.Trim(),
                    Dosage = dosage,
                    Frequency = selectedFreq.Content.ToString(),
                    Duration = duration,
                    StartDate = dpStartDate.SelectedDate.Value,
                    Notes = txtNotes.Text.Trim()
                };

                // сохранение
                int prescriptionId = prescriptionService.CreatePrescription(prescription);

                // расчёт даты окончания для отображения
                DateTime endDate = prescription.StartDate.AddDays(duration);

                MessageBox.Show(
                    string.Format(
                        "Назначение успешно создано!\n\n" +
                        "Тип: {0}\n" +
                        "Название: {1}\n" +
                        "{2}" +
                        "Период: {3:dd.MM.yyyy} - {4:dd.MM.yyyy} ({5} дн.)\n" +
                        "Статус: Активно",
                        prescriptionType,
                        prescription.Name,
                        !string.IsNullOrEmpty(dosage) ? string.Format("Дозировка: {0}\n", dosage) : "",
                        prescription.StartDate,
                        endDate,
                        duration),
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
            catch (ArgumentException ex)
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