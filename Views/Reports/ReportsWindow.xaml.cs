using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using PsychiatricHospitalWPF.Services;

namespace PsychiatricHospitalWPF.Views.Reports
{
    public partial class ReportsWindow : Window
    {
        private readonly ReportService reportService;
        private readonly PdfReportService pdfReportService;

        public ReportsWindow()
        {
            InitializeComponent();

            reportService = new ReportService();
            pdfReportService = new PdfReportService();

            LoadHospitalStatistics();
            LoadCurrentPatientsReport();
        }

        private void LoadHospitalStatistics()
        {
            try
            {
                var stats = reportService.GetHospitalStatistics();

                txtCurrentPatients.Text = stats.CurrentPatients.ToString();
                txtOccupancy.Text = string.Format("{0}/{1} ({2})",
                    stats.OccupiedBeds, stats.TotalBeds, stats.OccupancyRateDisplay);
                txtTotalDischarged.Text = stats.TotalDischarged.ToString();
                txtActivePrescriptions.Text = stats.ActivePrescriptions.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки статистики:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            // скрываем все отчёты
            dgCurrentPatients.Visibility = Visibility.Collapsed;
            dgWardOccupancy.Visibility = Visibility.Collapsed;
            gridDiagnosisStats.Visibility = Visibility.Collapsed;

            // скрываем легенду по умолчанию
            if (pnlLegend != null)
                pnlLegend.Visibility = Visibility.Collapsed;

            switch (cmbReportType.SelectedIndex)
            {
                case 0: // текущие пациенты
                    LoadCurrentPatientsReport();
                    dgCurrentPatients.Visibility = Visibility.Visible;
                    break;

                case 1: // загрузка палат
                    LoadWardOccupancyReport();
                    dgWardOccupancy.Visibility = Visibility.Visible;
                    // показываем легенду для отчёта по палатам
                    if (pnlLegend != null)
                        pnlLegend.Visibility = Visibility.Visible;
                    break;

                case 2: // статистика по диагнозам
                    LoadDiagnosisStatistics(null, null);
                    gridDiagnosisStats.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void LoadCurrentPatientsReport()
        {
            try
            {
                // очищаем предыдущие данные
                dgCurrentPatients.ItemsSource = null;

                var report = reportService.GetCurrentPatientsReport();

                // дополнительная фильтрация на случай дубликатов из БД
                var uniqueReport = new Dictionary<string, CurrentPatientReport>();
                foreach (var patient in report)
                {
                    if (!uniqueReport.ContainsKey(patient.CardNumber))
                    {
                        uniqueReport[patient.CardNumber] = patient;
                    }
                }

                var finalReport = new List<CurrentPatientReport>(uniqueReport.Values);
                dgCurrentPatients.ItemsSource = finalReport;

                lblStatus.Text = string.Format(
                    "Текущих пациентов: {0}. Отчёт сформирован: {1:dd.MM.yyyy HH:mm}",
                    finalReport.Count,
                    DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка формирования отчёта:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadWardOccupancyReport()
        {
            try
            {
                var report = reportService.GetWardOccupancyReport();
                dgWardOccupancy.ItemsSource = report;

                lblStatus.Text = string.Format(
                    "Всего палат: {0}. Отчёт сформирован: {1:dd.MM.yyyy HH:mm}",
                    report.Count,
                    DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка формирования отчёта:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadDiagnosisStatistics(DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                var stats = reportService.GetDiagnosisStatistics(dateFrom, dateTo);
                dgDiagnosisStats.ItemsSource = stats;

                string periodInfo = "";
                if (dateFrom.HasValue && dateTo.HasValue)
                    periodInfo = string.Format(" за период {0:dd.MM.yyyy} - {1:dd.MM.yyyy}",
                        dateFrom.Value, dateTo.Value);
                else if (dateFrom.HasValue)
                    periodInfo = string.Format(" с {0:dd.MM.yyyy}", dateFrom.Value);
                else if (dateTo.HasValue)
                    periodInfo = string.Format(" по {0:dd.MM.yyyy}", dateTo.Value);
                else
                    periodInfo = " за всё время";

                lblStatus.Text = string.Format(
                    "Диагнозов: {0}{1}. Отчёт сформирован: {2:dd.MM.yyyy HH:mm}",
                    stats.Count,
                    periodInfo,
                    DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка формирования статистики:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnApplyDiagFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagnosisStatistics(dpDiagFrom.SelectedDate, dpDiagTo.SelectedDate);
        }

        private void BtnResetDiagFilter_Click(object sender, RoutedEventArgs e)
        {
            // сбрасываем выбранные даты
            dpDiagFrom.SelectedDate = null;
            dpDiagTo.SelectedDate = null;

            // загружаем статистику за всё время
            LoadDiagnosisStatistics(null, null);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pdfPath = null;

                // показываем индикатор загрузки
                btnPrint.IsEnabled = false;
                lblStatus.Text = "Генерация PDF...";

                switch (cmbReportType.SelectedIndex)
                {
                    case 0: // текущие пациенты
                        pdfPath = pdfReportService.GenerateCurrentPatientsReport();
                        break;

                    case 1: // загрузка палат
                        pdfPath = pdfReportService.GenerateWardOccupancyReport();
                        break;

                    case 2: // статистика по диагнозам
                        pdfPath = pdfReportService.GenerateDiagnosisStatisticsReport(
                            dpDiagFrom.SelectedDate,
                            dpDiagTo.SelectedDate);
                        break;
                }

                if (pdfPath != null)
                {
                    // спрашиваем, хочет ли пользователь открыть файл
                    var result = MessageBox.Show(
                        string.Format("PDF-отчёт успешно создан!\n\nФайл сохранён:\n{0}\n\nОткрыть файл?", pdfPath),
                        "Успех",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // открываем PDF в программе по умолчанию
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = pdfPath,
                            UseShellExecute = true
                        });
                    }

                    lblStatus.Text = string.Format("PDF создан: {0}", System.IO.Path.GetFileName(pdfPath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка создания PDF:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                lblStatus.Text = "Ошибка создания PDF";
            }
            finally
            {
                btnPrint.IsEnabled = true;
            }
        }
    }
}