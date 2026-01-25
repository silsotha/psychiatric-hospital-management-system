using System.Windows;

namespace PsychiatricHospitalWPF.Views.Prescriptions
{
    public partial class CancelReasonWindow : Window
    {
        public string Reason { get; private set; }

        public CancelReasonWindow(string title = "Укажите причину отмены назначения")
        {
            InitializeComponent();

            txtTitle.Text = title;
            txtReason.Focus();

            // подписка на изменение текста для счетчика символов
            txtReason.TextChanged += TxtReason_TextChanged;
        }

        private void TxtReason_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            int charCount = txtReason.Text.Trim().Length;

            if (charCount >= 10)
            {
                lblCharCount.Text = string.Format("✓ {0} символов", charCount);
                lblCharCount.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                lblCharCount.Text = string.Format("Минимум 10 символов (введено: {0})", charCount);
                lblCharCount.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string text = txtReason.Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(
                    "Введите причину",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtReason.Focus();
                return;
            }

            if (text.Length < 10)
            {
                MessageBox.Show(
                    "Причина слишком короткая. Минимум 10 символов.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtReason.Focus();
                return;
            }

            Reason = text;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}