using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PsychiatricHospitalWPF.Views.Wards
{
    public partial class WardManagementWindow : Window
    {
        private readonly WardService wardService;
        private List<Ward> allWards;
        private Ward selectedWard;

        public WardManagementWindow()
        {
            InitializeComponent();

            wardService = new WardService();

            LoadWards();
        }

        private void LoadWards()
        {
            try
            {
                allWards = wardService.GetAllWards();
                dgWards.ItemsSource = allWards;

                // статистика
                int totalWards = allWards.Count;
                int totalBeds = 0;
                int occupiedBeds = 0;

                foreach (var ward in allWards)
                {
                    totalBeds += ward.TotalBeds;
                    occupiedBeds += ward.OccupiedBeds;
                }

                lblTotalStats.Text = string.Format(
                    "Всего палат: {0} | Всего мест: {1} | Занято: {2} | Свободно: {3} | Загрузка: {4:F1}%",
                    totalWards,
                    totalBeds,
                    occupiedBeds,
                    totalBeds - occupiedBeds,
                    totalBeds > 0 ? (double)occupiedBeds / totalBeds * 100 : 0);
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

        private void DgWards_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selectedWard = dgWards.SelectedItem as Ward;

            if (selectedWard != null)
            {
                LoadPatients(selectedWard.WardId);
                lblSelectedWard.Text = string.Format(
                    "Палата {0} - {1} ({2})",
                    selectedWard.WardNumber,
                    selectedWard.Department,
                    selectedWard.OccupancyDisplay);
                btnTransferPatient.Visibility = Visibility.Visible;
            }
            else
            {
                dgPatients.ItemsSource = null;
                lblSelectedWard.Text = "Выберите палату для просмотра пациентов";
                btnTransferPatient.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadPatients(int wardId)
        {
            try
            {
                var patients = wardService.GetPatientsByWard(wardId);
                dgPatients.ItemsSource = patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки пациентов:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DgPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // включаем кнопку перевода только если выбран пациент
            btnTransferPatient.IsEnabled = (dgPatients.SelectedItem != null);

            // показываем информацию о выбранном пациенте
            if (dgPatients.SelectedItem != null)
            {
                var patient = dgPatients.SelectedItem as Patient;
                lblSelectedInfo.Text = $"Выбран пациент: {patient.FullName}";
            }
            else
            {
                lblSelectedInfo.Text = "";
            }
        }

        private void BtnViewPatients_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null || button.Tag == null)
                return;

            int wardId = (int)button.Tag;

            // находим палату в списке
            var ward = allWards.Find(w => w.WardId == wardId);
            if (ward != null)
            {
                dgWards.SelectedItem = ward;
                dgWards.ScrollIntoView(ward);
            }
        }

        private void BtnTransferPatient_Click(object sender, RoutedEventArgs e)
        {
            var patient = dgPatients.SelectedItem as Patient;

            if (patient == null)
            {
                MessageBox.Show(
                    "Выберите пациента для перевода",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (selectedWard == null || !patient.WardId.HasValue)
            {
                MessageBox.Show(
                    "Невозможно определить текущую палату пациента",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // открываем окно выбора новой палаты
            var window = new TransferPatientWindow(
                patient.PatientId,
                patient.FullName,
                patient.WardId.Value,
                selectedWard.WardNumber);

            if (window.ShowDialog() == true)
            {
                // обновляем данные
                LoadWards();
                if (selectedWard != null)
                {
                    LoadPatients(selectedWard.WardId);
                }
            }
        }

        private void BtnDepartmentStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = wardService.GetDepartmentStats();

                if (stats.Count == 0)
                {
                    MessageBox.Show(
                        "Нет данных для отображения",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // формируем текст статистики
                string message = "СТАТИСТИКА ПО ОТДЕЛЕНИЯМ:\n\n";

                foreach (var stat in stats)
                {
                    message += string.Format(
                        "🏥 {0}\n" +
                        "   Палат: {1}\n" +
                        "   Всего мест: {2}\n" +
                        "   Занято: {3}\n" +
                        "   Свободно: {4}\n" +
                        "   Загрузка: {5}\n\n",
                        stat.Department,
                        stat.WardCount,
                        stat.TotalBeds,
                        stat.OccupiedBeds,
                        stat.AvailableBeds,
                        stat.OccupancyDisplay);
                }

                MessageBox.Show(
                    message,
                    "Статистика по отделениям",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка получения статистики:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadWards();
            if (selectedWard != null)
            {
                LoadPatients(selectedWard.WardId);
            }
        }
    }
}