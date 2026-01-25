using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Services;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Views.MedicalRecords
{
    public partial class MedicalCardWindow : Window
    {
        private readonly MedicalRecordService recordService;
        private readonly PatientService patientService;
        private readonly int patientId;
        private readonly string patientName;
        private List<MedicalRecord> allRecords;
        private List<MedicalRecord> filteredRecords;

        public MedicalCardWindow(int patientId, string patientName)
        {
            InitializeComponent();

            this.patientId = patientId;
            this.patientName = patientName;

            recordService = new MedicalRecordService();
            patientService = new PatientService();

            LoadPatientInfo();
            LoadRecords();

            btnAddRecord.IsEnabled = UserSession.CanEditMedicalRecords();
        }

        private void LoadPatientInfo()
        {
            try
            {
                var patient = patientService.GetPatientById(patientId);
                if (patient != null)
                {
                    txtPatientName.Text = string.Format("Медицинская карта: {0}", patient.FullName);

                    txtPatientInfo.Text = string.Format(
                        "Карта: {0} | Дата рождения: {1} | Поступление: {2:dd.MM.yyyy} | Статус: {3}",
                        patient.CardNumber,
                        patient.BirthDateWithAge,
                        patient.AdmissionDate,
                        patient.StatusDisplay);
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

        private void LoadRecords()
        {
            try
            {
                allRecords = recordService.GetRecordsByPatient(patientId);
                filteredRecords = new List<MedicalRecord>(allRecords);

                UpdateDisplay();
                UpdateRecordCount();

                if (allRecords.Count == 0)
                {
                    txtRecordDetails.Text = "История записей пуста. Добавьте первую запись.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Ошибка загрузки записей:\n{0}", ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateDisplay()
        {
            switch (cmbViewMode.SelectedIndex)
            {
                case 0: // список
                    ShowListView();
                    break;
                case 1: // временная шкала
                    ShowTimelineView();
                    break;
                case 2: // группировка
                    ShowGroupedView();
                    break;
            }
        }

        private void ShowListView()
        {
            dgRecordsList.Visibility = Visibility.Visible;
            timelineView.Visibility = Visibility.Collapsed;
            groupedView.Visibility = Visibility.Collapsed;

            // установка ItemsSource
            dgRecordsList.ItemsSource = null;
            dgRecordsList.ItemsSource = filteredRecords;
        }

        private void ShowTimelineView()
        {
            dgRecordsList.Visibility = Visibility.Collapsed;
            timelineView.Visibility = Visibility.Visible;
            groupedView.Visibility = Visibility.Collapsed;

            BuildTimeline();
        }

        private void ShowGroupedView()
        {
            dgRecordsList.Visibility = Visibility.Collapsed;
            timelineView.Visibility = Visibility.Collapsed;
            groupedView.Visibility = Visibility.Visible;

            BuildGroupedView();
        }

        // построение временной шкалы
        private void BuildTimeline()
        {
            timelineContainer.Children.Clear();

            if (filteredRecords.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "Нет записей для отображения",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                timelineContainer.Children.Add(emptyText);
                return;
            }

            foreach (var record in filteredRecords.OrderByDescending(r => r.RecordDate))
            {
                var timelineItem = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(103, 58, 183)),
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    Padding = new Thickness(0, 0, 0, 15),
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // дата и время
                var dateStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Top
                };

                dateStack.Children.Add(new TextBlock
                {
                    Text = record.RecordDate.ToString("dd.MM.yyyy"),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(103, 58, 183))
                });

                dateStack.Children.Add(new TextBlock
                {
                    Text = record.RecordDate.ToString("HH:mm"),
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 2, 0, 0)
                });

                dateStack.Children.Add(new TextBlock
                {
                    Text = record.DayOfWeek,
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                });

                Grid.SetColumn(dateStack, 0);
                grid.Children.Add(dateStack);

                // иконка
                var icon = new TextBlock
                {
                    Text = record.RecordTypeIcon,
                    FontSize = 32,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                Grid.SetColumn(icon, 1);
                grid.Children.Add(icon);

                // содержимое
                var contentStack = new StackPanel
                {
                    Margin = new Thickness(10, 0, 0, 0)
                };

                contentStack.Children.Add(new TextBlock
                {
                    Text = record.RecordTypeDisplay,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });

                contentStack.Children.Add(new TextBlock
                {
                    Text = string.Format("Врач: {0}", record.DoctorName),
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 3, 0, 5)
                });

                var descBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(5)
                };

                descBorder.Child = new TextBlock
                {
                    Text = record.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13
                };

                contentStack.Children.Add(descBorder);

                Grid.SetColumn(contentStack, 2);
                grid.Children.Add(contentStack);

                timelineItem.Child = grid;
                timelineContainer.Children.Add(timelineItem);
            }
        }

        // построение группированного представления
        private void BuildGroupedView()
        {
            // ItemsSource вручную
            var collectionView = new ListCollectionView(
                filteredRecords.OrderByDescending(r => r.RecordDate).ToList());

            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("DateGroup"));

            // отображение колонок (сброс старого)
            groupedView.View = null;

            var gridView = new GridView();

            // колонка "Время" (без даты, т.к. она в заголовке группы)
            var timeColumn = new GridViewColumn
            {
                Header = "Время",
                Width = 80,
                DisplayMemberBinding = new Binding("RecordDate")
                {
                    StringFormat = "HH:mm"
                }
            };
            gridView.Columns.Add(timeColumn);

            // колонка с иконкой
            var typeColumn = new GridViewColumn
            {
                Header = "Тип",
                Width = 150
            };
            var typeTemplate = new DataTemplate();
            var typeStack = new FrameworkElementFactory(typeof(StackPanel));
            typeStack.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);

            var typeIcon = new FrameworkElementFactory(typeof(TextBlock));
            typeIcon.SetBinding(TextBlock.TextProperty, new Binding("RecordTypeIcon"));
            typeIcon.SetValue(TextBlock.FontSizeProperty, 16.0);
            typeIcon.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 8, 0));
            typeStack.AppendChild(typeIcon);

            var typeText = new FrameworkElementFactory(typeof(TextBlock));
            typeText.SetBinding(TextBlock.TextProperty, new Binding("RecordTypeDisplay"));
            typeStack.AppendChild(typeText);

            typeTemplate.VisualTree = typeStack;
            typeColumn.CellTemplate = typeTemplate;
            gridView.Columns.Add(typeColumn);

            // колонка
            var doctorColumn = new GridViewColumn
            {
                Header = "Врач",
                Width = 200,
                DisplayMemberBinding = new Binding("DoctorName")
            };
            gridView.Columns.Add(doctorColumn);

            // колонка
            var descColumn = new GridViewColumn
            {
                Header = "Описание",
                Width = 400
            };
            var descTemplate = new DataTemplate();
            var descTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            descTextBlock.SetBinding(TextBlock.TextProperty, new Binding("Description"));
            descTextBlock.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            descTextBlock.SetValue(TextBlock.MaxWidthProperty, 380.0);
            descTemplate.VisualTree = descTextBlock;
            descColumn.CellTemplate = descTemplate;
            gridView.Columns.Add(descColumn);

            groupedView.View = gridView;

            // сначала в null, затем в новое
            groupedView.ItemsSource = null;
            groupedView.ItemsSource = collectionView;
        }

        private void UpdateRecordCount()
        {
            lblRecordCount.Text = string.Format(
                "Всего записей: {0}{1}",
                allRecords.Count,
                filteredRecords.Count != allRecords.Count
                    ? string.Format(" (отображается: {0})", filteredRecords.Count)
                    : "");
        }

        private void DgRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var record = dgRecordsList.SelectedItem as MedicalRecord;
            if (record != null)
            {
                DisplayRecordDetails(record);
            }
        }

        private void GroupedView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var record = groupedView.SelectedItem as MedicalRecord;
            if (record != null)
            {
                DisplayRecordDetails(record);
            }
        }

        private void DisplayRecordDetails(MedicalRecord record)
        {
            lblRecordIcon.Text = record.RecordTypeIcon;
            lblRecordDate.Text = string.Format("Дата: {0:dd.MM.yyyy HH:mm} ({1})",
                record.RecordDate, record.DayOfWeek);
            lblRecordType.Text = string.Format("Тип: {0}", record.RecordTypeDisplay);
            lblDoctor.Text = string.Format("Врач: {0}", record.DoctorName);
            txtRecordDetails.Text = record.Description;
        }

        private void CmbViewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateDisplay();
            }
        }

        private void BtnApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredRecords = new List<MedicalRecord>(allRecords);

            // фильтр по дате От
            if (dpFilterFrom.SelectedDate.HasValue)
            {
                filteredRecords = filteredRecords
                    .Where(r => r.RecordDate.Date >= dpFilterFrom.SelectedDate.Value.Date)
                    .ToList();
            }

            // фильтр по дате До
            if (dpFilterTo.SelectedDate.HasValue)
            {
                filteredRecords = filteredRecords
                    .Where(r => r.RecordDate.Date <= dpFilterTo.SelectedDate.Value.Date)
                    .ToList();
            }

            // фильтр по типу записи
            if (cmbFilterType.SelectedIndex > 0)
            {
                string selectedType = "";
                switch (cmbFilterType.SelectedIndex)
                {
                    case 1: selectedType = "осмотр"; break;
                    case 2: selectedType = "консультация"; break;
                    case 3: selectedType = "изменение_состояния"; break;
                    case 4: selectedType = "анализы"; break;
                    case 5: selectedType = "назначение"; break;
                }

                if (!string.IsNullOrEmpty(selectedType))
                {
                    filteredRecords = filteredRecords
                        .Where(r => r.RecordType != null && r.RecordType.ToLower() == selectedType)
                        .ToList();
                }
            }

            UpdateDisplay();
            UpdateRecordCount();

            if (filteredRecords.Count == 0)
            {
                MessageBox.Show(
                    "По заданным критериям записи не найдены.",
                    "Фильтрация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            dpFilterFrom.SelectedDate = null;
            dpFilterTo.SelectedDate = null;
            cmbFilterType.SelectedIndex = 0;

            filteredRecords = new List<MedicalRecord>(allRecords);
            UpdateDisplay();
            UpdateRecordCount();
        }

        private void BtnAddRecord_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddRecordWindow(patientId, patientName);
            if (window.ShowDialog() == true)
            {
                LoadRecords();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRecords();
        }

        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            ExportToPDF();
        }

        // экспорт истории болезни (через печать в PDF)
        private void ExportToPDF()
        {
            try
            {
                // создаем FlowDocument для печати
                var document = new FlowDocument();
                document.PagePadding = new Thickness(50);
                document.FontFamily = new FontFamily("Segoe UI");

                // заголовок
                var titlePara = new Paragraph(new Run("МЕДИЦИНСКАЯ КАРТА ПАЦИЕНТА"))
                {
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.DarkBlue
                };
                document.Blocks.Add(titlePara);

                // информация о пациенте
                var patient = patientService.GetPatientById(patientId);
                if (patient != null)
                {
                    var infoPara = new Paragraph
                    {
                        Margin = new Thickness(0, 0, 0, 20),
                        FontSize = 12
                    };

                    infoPara.Inlines.Add(new Run("ФИО: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(patient.FullName + "\n"));

                    infoPara.Inlines.Add(new Run("Номер карты: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(patient.CardNumber + "\n"));

                    infoPara.Inlines.Add(new Run("Дата рождения: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(patient.BirthDateWithAge + "\n"));

                    infoPara.Inlines.Add(new Run("Дата поступления: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(string.Format("{0:dd.MM.yyyy}\n", patient.AdmissionDate)));

                    infoPara.Inlines.Add(new Run("Диагноз: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(patient.Diagnosis + "\n"));

                    infoPara.Inlines.Add(new Run("Дата экспорта: ") { FontWeight = FontWeights.Bold });
                    infoPara.Inlines.Add(new Run(string.Format("{0:dd.MM.yyyy HH:mm}\n", DateTime.Now)));

                    document.Blocks.Add(infoPara);
                }

                // линия
                var line = new Paragraph(new Run(new string('_', 90)))
                {
                    Margin = new Thickness(0, 0, 0, 20),
                    Foreground = Brushes.Gray
                };
                document.Blocks.Add(line);

                // заголовок истории
                var historyTitle = new Paragraph(new Run("ИСТОРИЯ БОЛЕЗНИ"))
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15),
                    Foreground = Brushes.DarkBlue
                };
                document.Blocks.Add(historyTitle);

                // записи
                int recordNum = 1;
                foreach (var record in filteredRecords.OrderByDescending(r => r.RecordDate))
                {
                    var recordSection = new Section
                    {
                        Margin = new Thickness(0, 0, 0, 20)
                    };

                    // заголовок записи
                    var recordHeader = new Paragraph
                    {
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    recordHeader.Inlines.Add(new Run(string.Format("#{0}  ", recordNum++))
                    {
                        FontSize = 11,
                        Foreground = Brushes.Gray
                    });

                    recordHeader.Inlines.Add(new Run(record.RecordTypeIcon + " ")
                    {
                        FontSize = 16
                    });

                    recordHeader.Inlines.Add(new Run(
                        string.Format("{0:dd.MM.yyyy HH:mm} - {1}",
                        record.RecordDate,
                        record.RecordTypeDisplay))
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 13
                    });

                    recordSection.Blocks.Add(recordHeader);

                    // информация о враче
                    var doctorPara = new Paragraph(
                        new Run(string.Format("Врач: {0}", record.DoctorName)))
                    {
                        FontStyle = FontStyles.Italic,
                        Foreground = Brushes.Gray,
                        FontSize = 11,
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    recordSection.Blocks.Add(doctorPara);

                    // описание
                    var descPara = new Paragraph(new Run(record.Description))
                    {
                        FontSize = 12,
                        TextAlignment = TextAlignment.Justify,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    recordSection.Blocks.Add(descPara);

                    // разделитель
                    var separator = new Paragraph(new Run(new string('-', 90)))
                    {
                        Foreground = Brushes.LightGray,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    recordSection.Blocks.Add(separator);

                    document.Blocks.Add(recordSection);
                }

                // итоговая информация
                var footerPara = new Paragraph(
                    new Run(string.Format("\nВсего записей: {0}", filteredRecords.Count)))
                {
                    FontSize = 11,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                document.Blocks.Add(footerPara);

                // диалог печати
                var printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // печатаем документ (можно через "Microsoft Print to PDF" сохранять)
                    printDialog.PrintDocument(
                        ((IDocumentPaginatorSource)document).DocumentPaginator,
                        string.Format("МедКарта_{0}", patientName));

                    MessageBox.Show(
                        string.Format(
                            "Документ отправлен на печать!\n\n" +
                            "Если у вас установлен виртуальный принтер PDF " +
                            "(например, Microsoft Print to PDF),\n" +
                            "выберите его для сохранения в файл.\n\n" +
                            "Записей экспортировано: {0}",
                            filteredRecords.Count),
                        "Экспорт завершен",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(
                        "Ошибка экспорта:\n{0}\n\n" +
                        "Убедитесь, что установлен виртуальный принтер PDF\n" +
                        "(Microsoft Print to PDF должен быть в Windows 10/11)",
                        ex.Message),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}