using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PsychiatricHospitalWPF.Services;

namespace PsychiatricHospitalWPF.Views.Reports
{
    public partial class ReportsWindow : Window
    {
        private readonly ReportService reportService;

        public ReportsWindow()
        {
            InitializeComponent();

            reportService = new ReportService();

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

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FlowDocument document = null;

                switch (cmbReportType.SelectedIndex)
                {
                    case 0:
                        document = CreateCurrentPatientsDocument();
                        break;
                    case 1:
                        document = CreateWardOccupancyDocument();
                        break;
                    case 2:
                        document = CreateDiagnosisStatisticsDocument();
                        break;
                }

                if (document == null)
                    return;

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(
                        ((IDocumentPaginatorSource)document).DocumentPaginator,
                        "Отчёт больницы");

                    MessageBox.Show(
                        "Документ отправлен на печать!\n\n" +
                        "Если у вас установлен виртуальный принтер PDF\n" +
                        "(Microsoft Print to PDF), выберите его для сохранения.",
                        "Печать",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка печати:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateCurrentPatientsDocument()
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI")
            };

            // заголовок
            doc.Blocks.Add(new Paragraph(new Run("ОТЧЁТ О ТЕКУЩИХ ПАЦИЕНТАХ"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // дата формирования
            doc.Blocks.Add(new Paragraph(
                new Run(string.Format("Дата формирования: {0:dd.MM.yyyy HH:mm}",
                DateTime.Now)))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // статистика
            var stats = reportService.GetHospitalStatistics();
            doc.Blocks.Add(new Paragraph(
                new Run(string.Format("Текущих пациентов: {0}", stats.CurrentPatients)))
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // таблица
            var patients = dgCurrentPatients.ItemsSource as List<CurrentPatientReport>;
            if (patients != null && patients.Count > 0)
            {
                var table = new Table();
                table.Columns.Add(new TableColumn { Width = new GridLength(100) });
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });
                table.Columns.Add(new TableColumn { Width = new GridLength(50) });
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(70) });

                var rowGroup = new TableRowGroup();

                // заголовки
                var headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("№ Карты")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("ФИО")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Возр.")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Диагноз")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Поступил")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Дней")) { FontWeight = FontWeights.Bold }));
                rowGroup.Rows.Add(headerRow);

                // данные
                foreach (var patient in patients)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.CardNumber))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.FullName))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.Age.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.Diagnosis))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.AdmissionDateDisplay))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(patient.DaysInHospital.ToString()))));
                    rowGroup.Rows.Add(row);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);
            }

            return doc;
        }

        private FlowDocument CreateWardOccupancyDocument()
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI")
            };

            doc.Blocks.Add(new Paragraph(new Run("ОТЧЁТ О ЗАГРУЗКЕ ПАЛАТ"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            doc.Blocks.Add(new Paragraph(
                new Run(string.Format("Дата формирования: {0:dd.MM.yyyy HH:mm}", DateTime.Now)))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            });

            var wards = dgWardOccupancy.ItemsSource as List<WardOccupancyReport>;
            if (wards != null && wards.Count > 0)
            {
                var table = new Table();
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });
                table.Columns.Add(new TableColumn { Width = new GridLength(80) });

                var rowGroup = new TableRowGroup();

                var headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Отделение")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Палата")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Всего")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Занято")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Свободно")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Загрузка")) { FontWeight = FontWeights.Bold }));
                rowGroup.Rows.Add(headerRow);

                foreach (var ward in wards)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.Department))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.WardNumber))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.TotalBeds.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.OccupiedBeds.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.AvailableBeds.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(ward.OccupancyDisplay))));
                    rowGroup.Rows.Add(row);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);
            }

            return doc;
        }

        private FlowDocument CreateDiagnosisStatisticsDocument()
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(50),
                FontFamily = new FontFamily("Segoe UI")
            };

            doc.Blocks.Add(new Paragraph(new Run("СТАТИСТИКА ПО ДИАГНОЗАМ"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            doc.Blocks.Add(new Paragraph(
                new Run(string.Format("Дата формирования: {0:dd.MM.yyyy HH:mm}", DateTime.Now)))
            {
                FontSize = 10,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            });

            var diagStats = dgDiagnosisStats.ItemsSource as List<DiagnosisStatistics>;
            if (diagStats != null && diagStats.Count > 0)
            {
                var table = new Table();
                table.Columns.Add(new TableColumn { Width = new GridLength(300) });
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });
                table.Columns.Add(new TableColumn { Width = new GridLength(150) });

                var rowGroup = new TableRowGroup();

                var headerRow = new TableRow();
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Диагноз")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Пациентов")) { FontWeight = FontWeights.Bold }));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Средн. длительность")) { FontWeight = FontWeights.Bold }));
                rowGroup.Rows.Add(headerRow);

                foreach (var diag in diagStats)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(diag.Diagnosis))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(diag.PatientCount.ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(diag.AverageDurationDisplay))));
                    rowGroup.Rows.Add(row);
                }

                table.RowGroups.Add(rowGroup);
                doc.Blocks.Add(table);
            }

            return doc;
        }
    }
}