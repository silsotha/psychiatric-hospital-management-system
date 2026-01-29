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

        // валидация времени
        private void TxtExecutionTime_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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