using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using PsychiatricHospitalWPF.Views.MedicalRecords;
using PsychiatricHospitalWPF.Views.Prescriptions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PsychiatricHospitalWPF.Views.Patients
{
    public partial class PatientsListWindow : Window
    {
        private readonly PatientService patientService;

        public PatientsListWindow()
        {
            InitializeComponent();
            patientService = new PatientService();

            InitializeControls();

            if (!UserSession.CanAccessPatients())
            {
                btnAddPatient.IsEnabled = false;
            }

            LoadPatients();
        }

        private void InitializeControls()
        {
            if (cmbStatus.Items.Count > 0)
            {
                cmbStatus.SelectedIndex = 1;
            }

            cmbStatus.SelectionChanged += CmbStatus_SelectionChanged;
            btnSearch.Click += BtnSearch_Click;
            btnAddPatient.Click += BtnAddPatient_Click;
            btnRefresh.Click += BtnRefresh_Click;
            txtSearch.KeyDown += TxtSearch_KeyDown;
            dgPatients.PreviewMouseDoubleClick += DgPatients_PreviewMouseDoubleClick;
        }

        private void LoadPatients()
        {
            try
            {
                string status = null;

                if (cmbStatus != null && cmbStatus.SelectedIndex >= 0)
                {
                    if (cmbStatus.SelectedIndex == 1)
                        status = "active";
                    else if (cmbStatus.SelectedIndex == 2)
                        status = "discharged";
                }

                var patients = patientService.GetAllPatients(status);
                dgPatients.ItemsSource = patients;

                if (lblInfo != null)
                {
                    lblInfo.Text = string.Format("Всего пациентов: {0}", patients.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки данных:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                LoadPatients();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void BtnAdvancedSearch_Click(object sender, RoutedEventArgs e)
        {
            var window = new AdvancedSearchWindow();
            window.ShowDialog();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadPatients();
                return;
            }

            try
            {
                var patients = patientService.SearchPatients(txtSearch.Text.Trim());
                dgPatients.ItemsSource = patients;

                if (lblInfo != null)
                {
                    lblInfo.Text = string.Format("Найдено пациентов: {0}", patients.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка поиска:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            var window = new PatientEditWindow();
            if (window.ShowDialog() == true)
            {
                LoadPatients();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

        private void DgPatients_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(
                (DataGrid)sender,
                e.OriginalSource as DependencyObject) as DataGridRow;

            if (row != null)
            {
                OpenMedicalCard();
                e.Handled = true;
            }
        }

        private void BtnOpenMedCard_Click(object sender, RoutedEventArgs e)
        {
            OpenMedicalCard();
        }

        // выписка пациента
        private void BtnDischargePatient_Click(object sender, RoutedEventArgs e)
        {
            var patient = dgPatients.SelectedItem as Patient;
            if (patient == null)
            {
                // получаем пациента из контекста кнопки
                var button = sender as Button;
                if (button != null)
                {
                    patient = button.DataContext as Patient;
                }
            }

            if (patient == null)
            {
                MessageBox.Show(
                    "Пожалуйста, выберите пациента из списка",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // пациент уже выписан?
            if (patient.Status == "discharged")
            {
                MessageBox.Show(
                    "Этот пациент уже выписан!",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // проверка прав
            if (!UserSession.CanEditMedicalRecords())
            {
                MessageBox.Show(
                    "Недостаточно прав!\n\n" +
                    "Выписку пациента может оформить только врач.",
                    "Ошибка доступа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // открываем окно выписки
            var window = new DischargePatientWindow(
                patient.PatientId,
                patient.FullName,
                patient.Diagnosis);

            if (window.ShowDialog() == true)
            {
                // обновляем список после выписки
                LoadPatients();
            }
        }

        private void OpenMedicalCard()
        {
            var patient = dgPatients.SelectedItem as Patient;
            if (patient != null)
            {
                var window = new MedicalCardWindow(patient.PatientId, patient.FullName);
                window.Show();
            }
            else
            {
                MessageBox.Show(
                    "Пожалуйста, выберите пациента из списка",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        // открытие окна назначений пациента
        private void BtnPrescriptions_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null)
                return;

            int patientId = (int)button.Tag;

            try
            {
                // получаем информацию о пациенте
                var patient = patientService.GetPatientById(patientId);

                if (patient == null)
                {
                    MessageBox.Show(
                        "Пациент не найден",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // проверяем статус пациента
                if (patient.Status != "active")
                {
                    MessageBox.Show(
                        string.Format(
                            "Невозможно открыть назначения.\n\n" +
                            "Пациент: {0}\n" +
                            "Статус: {1}\n\n" +
                            "Назначения доступны только для активных пациентов.",
                            patient.FullName,
                            patient.StatusDisplay),
                        "Пациент не активен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // открываем окно назначений
                var prescriptionsWindow = new PrescriptionsWindow(patientId, patient.FullName);
                prescriptionsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка открытия окна назначений:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}