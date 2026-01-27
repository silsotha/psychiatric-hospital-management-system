using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Views.Wards
{
    public partial class TransferPatientWindow : Window
    {
        private readonly WardService wardService;
        private readonly int patientId;
        private readonly string patientName;
        private readonly int currentWardId;
        private readonly string currentWardNumber;

        public TransferPatientWindow(int patientId, string patientName,
                                     int currentWardId, string currentWardNumber)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;
            this.currentWardId = currentWardId;
            this.currentWardNumber = currentWardNumber;

            wardService = new WardService();

            txtPatientInfo.Text = string.Format("Пациент: {0}", patientName);
            txtCurrentWard.Text = string.Format("Палата {0}", currentWardNumber);

            LoadAvailableWards();
        }

        private void LoadAvailableWards()
        {
            try
            {
                cmbNewWard.Items.Clear();

                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // загружаем палаты со свободными местами, кроме текущей
                    string query = @"
                        SELECT WardId, WardNumber, Department, TotalBeds, OccupiedBeds
                        FROM Wards
                        WHERE OccupiedBeds < TotalBeds 
                          AND WardId != @CurrentWardId
                        ORDER BY WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CurrentWardId", currentWardId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var wardItem = new WardListItem
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

                                cmbNewWard.Items.Add(wardItem);
                            }
                        }
                    }
                }

                if (cmbNewWard.Items.Count == 0)
                {
                    MessageBox.Show(
                        "Нет доступных палат со свободными местами!\n\n" +
                        "Перевод невозможен.",
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

        private void BtnTransfer_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            // валидация
            if (cmbNewWard.SelectedItem == null)
            {
                ShowError("Выберите новую палату");
                cmbNewWard.Focus();
                return;
            }

            var selectedWard = cmbNewWard.SelectedItem as WardListItem;
            if (selectedWard == null)
            {
                ShowError("Ошибка выбора палаты");
                return;
            }

            // подтверждение
            var result = MessageBox.Show(
                string.Format(
                    "Перевести пациента?\n\n" +
                    "Пациент: {0}\n" +
                    "Из палаты: {1}\n" +
                    "В палату: {2}\n\n" +
                    "{3}",
                    patientName,
                    currentWardNumber,
                    selectedWard.WardNumber,
                    !string.IsNullOrWhiteSpace(txtReason.Text)
                        ? string.Format("Причина: {0}", txtReason.Text.Trim())
                        : "Причина не указана"),
                "Подтверждение перевода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                string reason = string.IsNullOrWhiteSpace(txtReason.Text)
                    ? "Не указана"
                    : txtReason.Text.Trim();

                wardService.TransferPatient(
                    patientId,
                    currentWardId,
                    selectedWard.WardId,
                    reason);

                MessageBox.Show(
                    string.Format(
                        "Пациент успешно переведен!\n\n" +
                        "Из палаты: {0}\n" +
                        "В палату: {1}",
                        currentWardNumber,
                        selectedWard.WardNumber),
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (InvalidOperationException ex)
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

        private class WardListItem
        {
            public int WardId { get; set; }
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