using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;
using PsychiatricHospitalWPF.Views.MedicalRecords;

namespace PsychiatricHospitalWPF.Views.Patients
{
    public partial class AdvancedSearchWindow : Window
    {
        private readonly PatientService patientService;

        public AdvancedSearchWindow()
        {
            InitializeComponent();
            patientService = new PatientService();

            LoadWards();

            // подписка на Enter в текстовых полях
            txtSearchName.KeyDown += SearchField_KeyDown;
            txtSearchCardNumber.KeyDown += SearchField_KeyDown;
        }

        // загрузка списка палат для фильтра
        private void LoadWards()
        {
            try
            {
                cmbSearchWard.Items.Clear();

                // опция поиска по всем палатам
                cmbSearchWard.Items.Add(new WardFilterItem
                {
                    WardNumber = null,
                    Display = "Все палаты"
                });

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT WardNumber
                        FROM Wards
                        ORDER BY WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var wardNumber = reader["WardNumber"].ToString();
                            cmbSearchWard.Items.Add(new WardFilterItem
                            {
                                WardNumber = wardNumber,
                                Display = string.Format("Палата {0}", wardNumber)
                            });
                        }
                    }
                }

                if (cmbSearchWard.Items.Count > 0)
                {
                    cmbSearchWard.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки палат:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                cmbSearchWard.Items.Clear();
                cmbSearchWard.Items.Add(new WardFilterItem
                {
                    WardNumber = null,
                    Display = "Все палаты"
                });
                cmbSearchWard.SelectedIndex = 0;
            }
        }

        // обработка нажатия Enter в полях поиска
        private void SearchField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        // выполнение расширенного поиска
        private void PerformSearch()
        {
            try
            {
                // получаем критерии поиска
                var searchName = txtSearchName.Text.Trim();
                var searchCardNumber = txtSearchCardNumber.Text.Trim();

                string searchWard = null;
                if (cmbSearchWard.SelectedItem is WardFilterItem selectedWard &&
                    selectedWard.WardNumber != null)
                {
                    searchWard = selectedWard.WardNumber;
                }

                string searchStatus = null;
                if (cmbSearchStatus.SelectedIndex == 1)
                    searchStatus = "active";
                else if (cmbSearchStatus.SelectedIndex == 2)
                    searchStatus = "discharged";

                // проверяем, что хотя бы один критерий заполнен
                if (string.IsNullOrEmpty(searchName) &&
                    string.IsNullOrEmpty(searchCardNumber) &&
                    searchWard == null)
                {
                    // если критерии не заполнены, показываем всех по статусу
                    var allPatients = patientService.GetAllPatients(searchStatus);
                    dgSearchResults.ItemsSource = allPatients;
                    lblSearchInfo.Text = string.Format(
                        "Показаны все пациенты. Найдено: {0}",
                        allPatients.Count);
                    return;
                }

                // выполняем расширенный поиск
                var results = patientService.AdvancedSearch(
                    searchName,
                    searchCardNumber,
                    searchWard,
                    searchStatus);

                dgSearchResults.ItemsSource = results;

                // формируем сообщение о результатах
                var criteriaUsed = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrEmpty(searchName))
                    criteriaUsed.Add(string.Format("ФИО: '{0}'", searchName));
                if (!string.IsNullOrEmpty(searchCardNumber))
                    criteriaUsed.Add(string.Format("Карта: '{0}'", searchCardNumber));
                if (searchWard != null)
                    criteriaUsed.Add(string.Format("Палата: {0}", searchWard));
                if (searchStatus != null)
                    criteriaUsed.Add(string.Format("Статус: {0}",
                        searchStatus == "active" ? "На лечении" : "Выписаны"));

                lblSearchInfo.Text = string.Format(
                    "Критерии: {0}. Найдено: {1}",
                    string.Join(", ", criteriaUsed),
                    results.Count);

                if (results.Count == 0)
                {
                    MessageBox.Show(
                        "По указанным критериям пациенты не найдены.\n\n" +
                        "Попробуйте изменить параметры поиска.",
                        "Результаты поиска",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
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

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            // очищаем все поля
            txtSearchName.Clear();
            txtSearchCardNumber.Clear();

            if (cmbSearchWard.Items.Count > 0)
                cmbSearchWard.SelectedIndex = 0;

            if (cmbSearchStatus.Items.Count > 0)
                cmbSearchStatus.SelectedIndex = 0;

            // очищаем результаты
            dgSearchResults.ItemsSource = null;
            lblSearchInfo.Text = "Фильтры сброшены. Введите критерии поиска и нажмите \"Найти\"";

            // фокус на первом поле
            txtSearchName.Focus();
        }

        private void DgSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenMedicalCard();
        }

        private void BtnOpenMedCard_Click(object sender, RoutedEventArgs e)
        {
            OpenMedicalCard();
        }

        // открытие медицинской карты выбранного пациента
        private void OpenMedicalCard()
        {
            var patient = dgSearchResults.SelectedItem as Patient;

            if (patient == null)
            {
                // пытаемся получить из контекста кнопки
                var button = dgSearchResults.SelectedItem as System.Windows.Controls.Button;
                if (button != null)
                {
                    patient = button.DataContext as Patient;
                }
            }

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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // вспомогательный класс для фильтра по палатам
        private class WardFilterItem
        {
            public string WardNumber { get; set; }
            public string Display { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}