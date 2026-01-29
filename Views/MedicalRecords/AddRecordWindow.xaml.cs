using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PsychiatricHospitalWPF.Views.MedicalRecords
{
    public partial class AddRecordWindow : Window
    {
        private readonly MedicalRecordService recordService;
        private readonly int patientId;
        private readonly string patientName;
        private bool isTimeUpdating = false; // флаг для предотвращения рекурсии

        public AddRecordWindow(int patientId, string patientName)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;

            recordService = new MedicalRecordService();

            txtPatientName.Text = string.Format("Пациент: {0}", patientName);

            // установка текущей даты и времени
            dpRecordDate.SelectedDate = DateTime.Now;
            dpRecordDate.DisplayDateEnd = DateTime.Now;
            txtExecutionTime.Text = DateTime.Now.ToString("HH:mm");

            // подписка на события для валидации времени
            txtExecutionTime.PreviewTextInput += txtExecutionTime_PreviewTextInput;
            txtExecutionTime.PreviewKeyDown += txtExecutionTime_PreviewKeyDown;
        }

        // валидация ввода времени - только цифры
        private void txtExecutionTime_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // разрешаем только цифры
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        // специальные клавиши
        private void txtExecutionTime_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // навигационные можно
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Tab || e.Key == Key.Home || e.Key == Key.End)
            {
                return;
            }
        }

        // валидация времени с обработкой курсора
        private void txtExecutionTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isTimeUpdating)
                return;

            string currentText = txtExecutionTime.Text;
            int currentCaretIndex = txtExecutionTime.CaretIndex;

            // убираем все нецифровые символы
            string digitsOnly = "";
            foreach (char c in currentText)
            {
                if (char.IsDigit(c))
                    digitsOnly += c;
            }

            if (string.IsNullOrEmpty(digitsOnly))
            {
                lblTimeHint.Text = "Введите время в формате ЧЧ:ММ (например, 14:30)";
                lblTimeHint.Foreground = Brushes.Gray;
                return;
            }

            // ограничиваем длину до 4 цифр
            if (digitsOnly.Length > 4)
            {
                digitsOnly = digitsOnly.Substring(0, 4);
            }

            // формируем новый текст
            string newText = "";
            bool hasError = false;
            string errorMessage = "";

            if (digitsOnly.Length >= 1)
            {
                // проверяем первую цифру часов
                int firstDigit = int.Parse(digitsOnly.Substring(0, 1));

                if (digitsOnly.Length >= 2)
                {
                    // проверяем полные часы
                    int hours = int.Parse(digitsOnly.Substring(0, 2));
                    if (hours > 23)
                    {
                        hasError = true;
                        errorMessage = "✗ Часы должны быть от 00 до 23";
                    }

                    newText = digitsOnly.Substring(0, 2) + ":";

                    if (digitsOnly.Length >= 3)
                    {
                        newText += digitsOnly.Substring(2);

                        if (digitsOnly.Length >= 4)
                        {
                            // проверяем минуты
                            int minutes = int.Parse(digitsOnly.Substring(2, 2));
                            if (minutes > 59)
                            {
                                hasError = true;
                                errorMessage = "✗ Минуты должны быть от 00 до 59";
                            }
                        }
                    }
                }
                else
                {
                    newText = digitsOnly;
                }
            }

            // обновляем поле, если текст изменился
            if (currentText != newText)
            {
                isTimeUpdating = true;

                txtExecutionTime.Text = newText;

                // обработка позиции курсора
                int newCaretIndex = currentCaretIndex;

                // если добавилось двоеточие и курсор был после второй цифры
                if (newText.Length > currentText.Length && newText.Contains(":"))
                {
                    int colonIndex = newText.IndexOf(':');
                    if (currentCaretIndex >= colonIndex)
                    {
                        newCaretIndex = currentCaretIndex + (newText.Length - currentText.Length);
                    }
                }

                // если удаляем символы
                if (newText.Length < currentText.Length)
                {
                    newCaretIndex = currentCaretIndex;
                }

                // курсор в допустимых пределах?
                if (newCaretIndex > newText.Length)
                    newCaretIndex = newText.Length;
                else if (newCaretIndex < 0)
                    newCaretIndex = 0;

                txtExecutionTime.CaretIndex = newCaretIndex;
                isTimeUpdating = false;
            }

            // обновляем подсказку
            if (hasError)
            {
                lblTimeHint.Text = errorMessage;
                lblTimeHint.Foreground = Brushes.Red;
            }
            else if (digitsOnly.Length == 4)
            {
                // проверяем полный формат
                if (TimeSpan.TryParse(newText, out TimeSpan time))
                {
                    lblTimeHint.Text = "✓ Формат времени корректен";
                    lblTimeHint.Foreground = Brushes.Green;
                }
                else
                {
                    lblTimeHint.Text = "✗ Неверное время!";
                    lblTimeHint.Foreground = Brushes.Red;
                }
            }
            else if (digitsOnly.Length >= 3)
            {
                lblTimeHint.Text = "Введите вторую цифру минут...";
                lblTimeHint.Foreground = Brushes.Orange;
            }
            else if (digitsOnly.Length == 2)
            {
                lblTimeHint.Text = "Введите минуты (00-59)";
                lblTimeHint.Foreground = Brushes.Gray;
            }
            else
            {
                lblTimeHint.Text = "Введите вторую цифру часов...";
                lblTimeHint.Foreground = Brushes.Gray;
            }
        }

        private void TxtDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            int charCount = txtDescription.Text.Length;
            lblCharCount.Text = string.Format("Символов: {0} (минимум 10)", charCount);

            if (charCount >= 10)
            {
                lblCharCount.Foreground = Brushes.Green;
            }
            else
            {
                lblCharCount.Foreground = Brushes.Gray;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // проверка прав
            if (!UserSession.CanEditMedicalRecords())
            {
                ShowError("Недостаточно прав для добавления записей. Требуется роль врача.");
                return;
            }

            // валидация даты
            if (!dpRecordDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату записи");
                dpRecordDate.Focus();
                return;
            }

            if (dpRecordDate.SelectedDate.Value > DateTime.Now)
            {
                ShowError("Дата записи не может быть в будущем");
                dpRecordDate.Focus();
                return;
            }

            // валидация времени
            if (string.IsNullOrWhiteSpace(txtExecutionTime.Text))
            {
                ShowError("Введите время записи");
                txtExecutionTime.Focus();
                return;
            }

            // проверка формата времени
            if (txtExecutionTime.Text.Length != 5 || !txtExecutionTime.Text.Contains(":"))
            {
                ShowError("Введите время полностью в формате ЧЧ:ММ (например, 14:30)");
                txtExecutionTime.Focus();
                return;
            }

            TimeSpan recordTime;
            if (!TimeSpan.TryParse(txtExecutionTime.Text.Trim(), out recordTime))
            {
                ShowError("Неверный формат времени! Используйте формат ЧЧ:ММ (например, 14:30)");
                txtExecutionTime.Focus();
                return;
            }

            // валидация типа записи
            if (cmbRecordType.SelectedItem == null)
            {
                ShowError("Выберите тип записи");
                cmbRecordType.Focus();
                return;
            }

            // валидация описания
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                ShowError("Введите описание состояния");
                txtDescription.Focus();
                return;
            }

            if (txtDescription.Text.Trim().Length < 10)
            {
                ShowError("Описание слишком короткое. Минимум 10 символов.");
                txtDescription.Focus();
                return;
            }

            try
            {
                // формируем полную дату и время
                DateTime recordDateTime = dpRecordDate.SelectedDate.Value.Date + recordTime;

                // проверка, что дата-время не в будущем
                if (recordDateTime > DateTime.Now)
                {
                    ShowError("Дата и время записи не могут быть в будущем");
                    return;
                }

                // получаем тип записи из Tag
                var selectedItem = cmbRecordType.SelectedItem as ComboBoxItem;
                string recordType = selectedItem.Tag.ToString();

                var record = new MedicalRecord
                {
                    PatientId = patientId,
                    RecordDate = recordDateTime,
                    Description = txtDescription.Text.Trim(),
                    RecordType = recordType
                };

                recordService.AddRecord(record);

                MessageBox.Show(
                    string.Format(
                        "Медицинская запись успешно добавлена!\n\n" +
                        "Тип: {0}\n" +
                        "Дата: {1:dd.MM.yyyy HH:mm}",
                        selectedItem.Content.ToString().Substring(3), // убираем иконку
                        recordDateTime),
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