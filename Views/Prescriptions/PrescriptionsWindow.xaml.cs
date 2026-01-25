using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PsychiatricHospitalWPF.Views.Prescriptions
{
    public partial class PrescriptionsWindow : Window
    {
        private readonly PrescriptionService prescriptionService;
        private readonly PatientService patientService;
        private readonly int patientId;
        private readonly string patientName;
        private List<Prescription> allPrescriptions;

        public PrescriptionsWindow(int patientId, string patientName)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;

            prescriptionService = new PrescriptionService();
            patientService = new PatientService();

            LoadPatientInfo();
            LoadPrescriptions();

            // скрыть кнопку добавления для медсестер
            if (UserSession.CurrentUser.Role == "nurse")
            {
                btnAddPrescription.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadPatientInfo()
        {
            try
            {
                var patient = patientService.GetPatientById(patientId);
                if (patient != null)
                {
                    txtPatientInfo.Text = string.Format(
                        "Пациент: {0} | Карта: {1} | Палата: {2}",
                        patient.FullName,
                        patient.CardNumber,
                        patient.WardNumber ?? "Не назначена");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки данных пациента:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadPrescriptions()
        {
            try
            {
                allPrescriptions = prescriptionService.GetPrescriptionsByPatient(patientId);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки назначений:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                allPrescriptions = new List<Prescription>();
                dgPrescriptions.ItemsSource = allPrescriptions;
            }
        }


        // обработчик загрузки строки - скрываем кнопки для неактивных назначений
        private void DgPrescriptions_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var prescription = e.Row.Item as Prescription;
            if (prescription == null)
                return;

            // ищем кнопки в строке
            var presenter = GetVisualChild<DataGridCellsPresenter>(e.Row);
            if (presenter == null)
            {
                e.Row.Loaded += (s, args) => HideButtonsForInactiveRow(e.Row);
                return;
            }

            HideButtonsForInactiveRow(e.Row);
        }

        // скрываем кнопки для неактивных назначений
        private void HideButtonsForInactiveRow(DataGridRow row)
        {
            var prescription = row.Item as Prescription;
            if (prescription == null || prescription.Status == "Активно")
                return;

            // ищем StackPanel с кнопками
            var presenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null)
                return;

            // перебираем ячейки
            for (int i = 0; i < dgPrescriptions.Columns.Count; i++)
            {
                var cell = presenter.ItemContainerGenerator.ContainerFromIndex(i) as DataGridCell;
                if (cell == null)
                    continue;

                var stackPanel = FindVisualChild<StackPanel>(cell);
                if (stackPanel != null)
                {
                    // скрываем весь StackPanel с кнопками
                    stackPanel.Visibility = Visibility.Collapsed;
                    break;
                }
            }
        }

        // вспомогательный метод для поиска визуального потомка
        private T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        // вспомогательный метод для поиска потомка по типу
        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void ApplyFilter()
        {
            if (allPrescriptions == null)
                return;

            List<Prescription> filtered = new List<Prescription>(allPrescriptions);

            switch (cmbStatusFilter.SelectedIndex)
            {
                case 1:
                    filtered = allPrescriptions.Where(p => p.Status == "Активно").ToList();
                    break;
                case 2:
                    filtered = allPrescriptions.Where(p => p.Status == "Завершено").ToList();
                    break;
                case 3:
                    filtered = allPrescriptions.Where(p => p.Status == "Отменено").ToList();
                    break;
                    // case 0 или default: все назначения
            }

            dgPrescriptions.ItemsSource = filtered;

            // обновляем заголовок с количеством
            this.Title = string.Format("Назначения пациента ({0} шт.)", filtered.Count);
        }

        private void BtnAddPrescription_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreatePrescriptionWindow(patientId, patientName);
            if (window.ShowDialog() == true)
            {
                LoadPrescriptions();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPrescriptions();
        }

        private void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyFilter();
            }
        }

        private void DgPrescriptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var prescription = dgPrescriptions.SelectedItem as Prescription;

            if (prescription != null)
            {
                ShowPrescriptionDetails(prescription);
            }
            else
            {
                HidePrescriptionDetails();
            }
        }

        private void ShowPrescriptionDetails(Prescription prescription)
        {
            panelDetails.Visibility = Visibility.Visible;
            txtNoSelection.Visibility = Visibility.Collapsed;

            txtDetailType.Text = string.Format("{0} {1}",
                prescription.TypeIcon, prescription.PrescriptionType);
            txtDetailFrequency.Text = prescription.Frequency;
            txtDetailDuration.Text = string.Format("{0} дней", prescription.Duration);
            txtDetailCreated.Text = prescription.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            txtDetailDoctor.Text = prescription.DoctorName;
            txtDetailExecutions.Text = string.Format("{0} раз", prescription.ExecutionCount);

            if (!string.IsNullOrEmpty(prescription.Notes))
            {
                txtDetailNotes.Text = string.Format("Примечания: {0}", prescription.Notes);
                txtDetailNotes.Visibility = Visibility.Visible;
            }
            else
            {
                txtDetailNotes.Visibility = Visibility.Collapsed;
            }

            // если отменено, показываем причину
            if (prescription.Status == "Отменено" && !string.IsNullOrEmpty(prescription.CancelReason))
            {
                txtDetailNotes.Text = string.Format("Причина отмены: {0}", prescription.CancelReason);
                txtDetailNotes.Visibility = Visibility.Visible;
            }
        }

        private void HidePrescriptionDetails()
        {
            panelDetails.Visibility = Visibility.Collapsed;
            txtNoSelection.Visibility = Visibility.Visible;
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int prescriptionId = (int)button.Tag;

            // получаем информацию о назначении
            var prescription = allPrescriptions.FirstOrDefault(p => p.PrescriptionId == prescriptionId);
            if (prescription == null)
                return;

            // подтверждение выполнения
            var result = MessageBox.Show(
                string.Format(
                    "Отметить выполнение назначения?\n\n" +
                    "{0} {1}\n" +
                    "Периодичность: {2}\n\n" +
                    "Текущее время: {3:HH:mm}",
                    prescription.TypeIcon,
                    prescription.FullName,
                    prescription.Frequency,
                    DateTime.Now),
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // отмечаем выполнение
                prescriptionService.ExecutePrescription(prescriptionId);

                MessageBox.Show(
                    string.Format(
                        "Выполнение отмечено!\n\n" +
                        "Назначение: {0}\n" +
                        "Время: {1:dd.MM.yyyy HH:mm}\n" +
                        "Медсестра/Врач: {2}",
                        prescription.Name,
                        DateTime.Now,
                        UserSession.CurrentUser.FullName),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // обновляем список
                LoadPrescriptions();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка отметки выполнения:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int prescriptionId = (int)button.Tag;

            // получаем информацию о назначении
            var prescription = allPrescriptions.FirstOrDefault(p => p.PrescriptionId == prescriptionId);
            if (prescription == null)
                return;

            // проверяем права (только создавший врач)
            if (prescription.DoctorId != UserSession.CurrentUser.UserId)
            {
                MessageBox.Show(
                    "Только врач, создавший назначение, может его отменить",
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // если есть выполнения, предлагаем завершить досрочно
            if (prescription.ExecutionCount > 0)
            {
                var choice = MessageBox.Show(
                    string.Format(
                        "Назначение имеет {0} отметок о выполнении.\n\n" +
                        "Отменить назначение нельзя, но можно завершить досрочно.\n\n" +
                        "Завершить назначение досрочно?",
                        prescription.ExecutionCount),
                    "Назначение выполняется",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (choice == MessageBoxResult.Yes)
                {
                    CompleteEarly(prescriptionId, prescription);
                }
                return;
            }

            // причина отмены?
            var cancelWindow = new CancelReasonWindow();
            if (cancelWindow.ShowDialog() != true)
                return;

            string cancelReason = cancelWindow.Reason;

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                MessageBox.Show(
                    "Необходимо указать причину отмены",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                prescriptionService.CancelPrescription(prescriptionId, cancelReason);

                MessageBox.Show(
                    string.Format(
                        "Назначение отменено\n\n" +
                        "Причина: {0}",
                        cancelReason),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadPrescriptions();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Доступ запрещен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка отмены назначения:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CompleteEarly(int prescriptionId, Prescription prescription)
        {
            var reasonWindow = new CancelReasonWindow("Укажите причину досрочного завершения:");
            if (reasonWindow.ShowDialog() != true)
                return;

            string reason = reasonWindow.Reason;

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(
                    "Необходимо указать причину завершения",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                prescriptionService.CompletePrescription(prescriptionId, reason);

                MessageBox.Show(
                    string.Format(
                        "Назначение завершено досрочно\n\n" +
                        "Причина: {0}",
                        reason),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LoadPrescriptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка завершения назначения:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}