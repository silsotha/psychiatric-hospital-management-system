using System;
using System.Data.SqlClient;
using System.Windows;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Views.Patients
{
    public partial class PatientEditWindow : Window
    {
        private readonly PatientService patientService;

        public PatientEditWindow()
        {
            InitializeComponent();
            patientService = new PatientService();

            dpBirthDate.DisplayDateEnd = DateTime.Now;
            dpBirthDate.SelectedDate = DateTime.Now.AddYears(-30);

            LoadWards();
        }

        private void LoadWards()
        {
            try
            {
                cmbWard.Items.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT WardId, WardNumber, Department, TotalBeds, OccupiedBeds
                        FROM Wards
                        WHERE OccupiedBeds < TotalBeds
                        ORDER BY WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var wardItem = new WardItem
                            {
                                WardId = (int)reader["WardId"],
                                WardNumber = reader["WardNumber"].ToString(),
                                Department = reader["Department"].ToString(),
                                TotalBeds = (int)reader["TotalBeds"],
                                OccupiedBeds = (int)reader["OccupiedBeds"],
                                Display = string.Format("{0} - {1} (Свободно: {2}/{3})",
                                    reader["WardNumber"],
                                    reader["Department"],
                                    (int)reader["TotalBeds"] - (int)reader["OccupiedBeds"],
                                    reader["TotalBeds"])
                            };

                            cmbWard.Items.Add(wardItem);
                        }
                    }
                }

                if (cmbWard.Items.Count > 0)
                {
                    cmbWard.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show(
                        "Нет доступных палат со свободными местами!\n\n" +
                        "Невозможно добавить нового пациента.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    this.DialogResult = false;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки палат:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                this.DialogResult = false;
                this.Close();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // валидация ФИО
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowError("Введите ФИО пациента");
                txtFullName.Focus();
                return;
            }

            // валидация даты рождения
            if (!dpBirthDate.SelectedDate.HasValue)
            {
                ShowError("Выберите дату рождения");
                return;
            }

            if (dpBirthDate.SelectedDate.Value > DateTime.Now)
            {
                ShowError("Дата рождения не может быть в будущем");
                return;
            }

            // диагноз обязателен
            if (string.IsNullOrWhiteSpace(txtDiagnosis.Text))
            {
                ShowError("Введите предварительный диагноз");
                txtDiagnosis.Focus();
                return;
            }

            // палата обязательна
            if (cmbWard.SelectedItem == null)
            {
                ShowError("Выберите палату для размещения пациента");
                cmbWard.Focus();
                return;
            }

            var selectedWard = cmbWard.SelectedItem as WardItem;
            if (selectedWard == null || !selectedWard.WardId.HasValue)
            {
                ShowError("Выберите палату для размещения пациента");
                cmbWard.Focus();
                return;
            }

            // проверка заполненности палаты
            if (!CheckWardAvailability(selectedWard.WardId.Value))
            {
                ShowError(string.Format(
                    "Палата {0} уже полностью занята!\n\n" +
                    "Пожалуйста, выберите другую палату или обновите список палат.",
                    selectedWard.WardNumber));
                return;
            }

            try
            {
                var patient = new Patient
                {
                    FullName = txtFullName.Text.Trim(),
                    BirthDate = dpBirthDate.SelectedDate.Value,
                    ContactInfo = string.IsNullOrWhiteSpace(txtContactInfo.Text)
                        ? null : txtContactInfo.Text.Trim(),
                    AdmissionDate = DateTime.Now,
                    Diagnosis = txtDiagnosis.Text.Trim(),
                    Status = "active",
                    WardId = selectedWard.WardId.Value
                };

                patientService.CreatePatient(patient);

                MessageBox.Show(
                    string.Format(
                        "Пациент успешно зарегистрирован!\n\n" +
                        "Номер карты: {0}\n" +
                        "Палата: {1}",
                        patient.CardNumber,
                        selectedWard.WardNumber),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError(string.Format("Ошибка: {0}", ex.Message));
            }
        }

        private bool CheckWardAvailability(int wardId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT TotalBeds, OccupiedBeds
                        FROM Wards
                        WHERE WardId = @WardId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WardId", wardId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int totalBeds = (int)reader["TotalBeds"];
                                int occupiedBeds = (int)reader["OccupiedBeds"];

                                return occupiedBeds < totalBeds;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка проверки палаты:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return false;
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

        private class WardItem
        {
            public int? WardId { get; set; }
            public string WardNumber { get; set; }
            public string Department { get; set; }
            public int TotalBeds { get; set; }
            public int OccupiedBeds { get; set; }
            public string Display { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}