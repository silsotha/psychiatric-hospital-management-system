using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using PsychiatricHospitalWPF.Views.MedicalRecords;
using PsychiatricHospitalWPF.Views.Prescriptions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PsychiatricHospitalWPF.Views.Patients
{
    public partial class PatientsListWindow : Window
    {
        private readonly PatientService patientService;
        private List<Patient> allPatients; // полный список пациентов
        private string currentStatusFilter = "active"; // текущий фильтр статуса

        public PatientsListWindow()
        {
            InitializeComponent();
            patientService = new PatientService();

            InitializeControls();

            if (!UserSession.CanAccessPatients())
            {
                btnAddPatient.IsEnabled = false;
            }

            LoadWards(); // загружаем список палат для фильтра
            LoadPatients();
        }

        private void InitializeControls()
        {
            // начальный фильтр "На лечении" (индекс 1)
            if (cmbStatus.Items.Count > 0)
            {
                cmbStatus.SelectedIndex = 1;
            }

            // подписываемся на события
            cmbStatus.SelectionChanged += CmbStatus_SelectionChanged;
            cmbWardFilter.SelectionChanged += CmbWardFilter_SelectionChanged;
            btnAddPatient.Click += BtnAddPatient_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnClearSearch.Click += BtnClearSearch_Click;

            // подписываемся на изменения в полях поиска
            txtSearchName.TextChanged += SearchField_TextChanged;
            txtSearchCard.TextChanged += SearchField_TextChanged;
            txtSearchDiagnosis.TextChanged += SearchField_TextChanged;

            // enter для всех полей поиска
            txtSearchName.KeyDown += SearchField_KeyDown;
            txtSearchCard.KeyDown += SearchField_KeyDown;
            txtSearchDiagnosis.KeyDown += SearchField_KeyDown;

            dgPatients.PreviewMouseDoubleClick += DgPatients_PreviewMouseDoubleClick;
        }

        /// <summary>
        /// загрузка списка палат для фильтра
        /// </summary>
        private void LoadWards()
        {
            try
            {
                // очищаем ComboBox (оставляем только "Все палаты")
                while (cmbWardFilter.Items.Count > 1)
                {
                    cmbWardFilter.Items.RemoveAt(1);
                }

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT WardNumber, Department
                        FROM Wards
                        ORDER BY WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var wardItem = new ComboBoxItem
                            {
                                Content = string.Format("{0} - {1}",
                                    reader["WardNumber"],
                                    reader["Department"]),
                                Tag = reader["WardNumber"].ToString()
                            };

                            cmbWardFilter.Items.Add(wardItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки палат:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// загрузка всех пациентов из БД
        /// </summary>
        private void LoadPatients()
        {
            try
            {
                // загружаем ВСЕ пациенты из БД (без фильтра)
                allPatients = patientService.GetAllPatients(null);

                // применяем текущие фильтры (статус + поиск + палата)
                ApplyFilters();
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

        /// <summary>
        /// единый метод применения всех фильтров
        /// </summary>
        private void ApplyFilters()
        {
            if (allPatients == null)
                return;

            var filtered = new List<Patient>(allPatients);

            // 1. фильтр по статусу (из ComboBox)
            if (currentStatusFilter == "active")
            {
                filtered = filtered.Where(p => p.Status == "active").ToList();
            }
            else if (currentStatusFilter == "discharged")
            {
                filtered = filtered.Where(p => p.Status == "discharged").ToList();
            }
            // если "all" - не фильтруем

            // 2. раздельные фильтры поиска
            string searchName = txtSearchName?.Text?.Trim();
            string searchCard = txtSearchCard?.Text?.Trim();
            string searchDiagnosis = txtSearchDiagnosis?.Text?.Trim();

            // 3. фильтр по палате
            string selectedWard = null;
            if (cmbWardFilter != null && cmbWardFilter.SelectedIndex > 0)
            {
                var selectedItem = cmbWardFilter.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    selectedWard = selectedItem.Tag.ToString();
                }
            }

            bool hasSearchFilters = false;
            var searchParts = new List<string>();

            // фильтр по ФИО (точное совпадение подстроки)
            if (!string.IsNullOrEmpty(searchName))
            {
                hasSearchFilters = true;
                // разбиваем ФИО на слова для более точного поиска
                var nameWords = searchName.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                filtered = filtered.Where(p =>
                {
                    if (p.FullName == null) return false;
                    string fullNameLower = p.FullName.ToLower();

                    // проверяем, что ВСЕ введённые слова присутствуют в ФИО
                    return nameWords.All(word => fullNameLower.Contains(word));
                }).ToList();

                searchParts.Add($"ФИО: \"{searchName}\"");
            }

            // фильтр по номеру карты (точное совпадение подстроки)
            if (!string.IsNullOrEmpty(searchCard))
            {
                hasSearchFilters = true;
                string searchCardLower = searchCard.ToLower();

                filtered = filtered.Where(p =>
                    p.CardNumber != null &&
                    p.CardNumber.ToLower().Contains(searchCardLower)
                ).ToList();

                searchParts.Add($"Карта: \"{searchCard}\"");
            }

            // фильтр по диагнозу (точное совпадение подстроки)
            if (!string.IsNullOrEmpty(searchDiagnosis))
            {
                hasSearchFilters = true;
                string searchDiagnosisLower = searchDiagnosis.ToLower();

                filtered = filtered.Where(p =>
                    p.Diagnosis != null &&
                    p.Diagnosis.ToLower().Contains(searchDiagnosisLower)
                ).ToList();

                searchParts.Add($"Диагноз: \"{searchDiagnosis}\"");
            }

            // фильтр по палате
            if (!string.IsNullOrEmpty(selectedWard))
            {
                hasSearchFilters = true;

                filtered = filtered.Where(p =>
                    p.WardNumber != null &&
                    p.WardNumber == selectedWard
                ).ToList();

                searchParts.Add($"Палата: {selectedWard}");
            }

            // 4. обновляем DataGrid
            dgPatients.ItemsSource = null; // очищаем перед обновлением
            dgPatients.ItemsSource = filtered;

            // 5. обновляем счётчики
            if (lblInfo != null)
            {
                if (hasSearchFilters)
                {
                    lblInfo.Text = string.Format("Найдено пациентов: {0}", filtered.Count);
                }
                else
                {
                    lblInfo.Text = string.Format("Всего пациентов: {0}", filtered.Count);
                }
            }

            // 6. показываем активные фильтры поиска
            if (lblSearchInfo != null)
            {
                if (searchParts.Count > 0)
                {
                    lblSearchInfo.Text = "🔍 Активные фильтры: " + string.Join(", ", searchParts);
                }
                else
                {
                    lblSearchInfo.Text = "";
                }
            }
        }

        /// <summary>
        /// обработчик изменения фильтра статуса
        /// </summary>
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // определяем текущий фильтр по индексу ComboBox
            switch (cmbStatus.SelectedIndex)
            {
                case 0: // все
                    currentStatusFilter = "all";
                    break;
                case 1: // на лечении
                    currentStatusFilter = "active";
                    break;
                case 2: // выписаны
                    currentStatusFilter = "discharged";
                    break;
                default:
                    currentStatusFilter = "active";
                    break;
            }

            // применяем все фильтры (статус + поиск + палата)
            ApplyFilters();
        }

        /// <summary>
        /// обработчик изменения фильтра по палате
        /// </summary>
        private void CmbWardFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            // применяем все фильтры
            ApplyFilters();
        }

        /// <summary>
        /// обработчик изменения текста в любом поле поиска
        /// </summary>
        private void SearchField_TextChanged(object sender, TextChangedEventArgs e)
        {
            // применяем фильтры при каждом изменении текста
            ApplyFilters();
        }

        /// <summary>
        /// обработчик нажатия Enter в полях поиска
        /// </summary>
        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// очистка всех полей поиска и фильтров
        /// </summary>
        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchName.Text = string.Empty;
            txtSearchCard.Text = string.Empty;
            txtSearchDiagnosis.Text = string.Empty;

            // сбрасываем фильтр палаты на "Все палаты"
            if (cmbWardFilter.Items.Count > 0)
            {
                cmbWardFilter.SelectedIndex = 0;
            }

            // фильтры применятся автоматически через TextChanged и SelectionChanged
        }

        /// <summary>
        /// добавление нового пациента
        /// </summary>
        private void BtnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            var window = new PatientEditWindow();
            if (window.ShowDialog() == true)
            {
                LoadWards(); // обновляем список палат
                LoadPatients();
            }
        }

        /// <summary>
        /// обновление списка
        /// </summary>
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // очищаем все поля поиска
            txtSearchName.Text = string.Empty;
            txtSearchCard.Text = string.Empty;
            txtSearchDiagnosis.Text = string.Empty;

            // сбрасываем фильтр палаты
            if (cmbWardFilter.Items.Count > 0)
            {
                cmbWardFilter.SelectedIndex = 0;
            }

            // перезагружаем данные
            LoadWards();
            LoadPatients();
        }

        /// <summary>
        /// двойной клик по строке - открытие медкарты
        /// </summary>
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

        /// <summary>
        /// открытие медицинской карты
        /// </summary>
        private void BtnOpenMedCard_Click(object sender, RoutedEventArgs e)
        {
            OpenMedicalCard();
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

        /// <summary>
        /// открытие окна назначений пациента
        /// </summary>
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

        /// <summary>
        /// выписка пациента
        /// </summary>
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
    }
}