using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PsychiatricHospitalWPF.Services;

namespace PsychiatricHospitalWPF.Services
{
    /// <summary>
    /// сервис для генерации PDF-отчётов с использованием Python reportlab
    /// </summary>
    public class PdfReportService
    {
        private readonly ReportService reportService;

        public PdfReportService()
        {
            reportService = new ReportService();
        }

        /// <summary>
        /// генерирует PDF-отчёт о текущих пациентах
        /// </summary>
        public string GenerateCurrentPatientsReport()
        {
            try
            {
                var patients = reportService.GetCurrentPatientsReport();
                var stats = reportService.GetHospitalStatistics();

                // создаём временный Python-скрипт
                string scriptPath = Path.Combine(Path.GetTempPath(), "generate_patients_report.py");
                string pdfPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    string.Format("Отчёт_Пациенты_{0:yyyyMMdd_HHmmss}.pdf", DateTime.Now));

                // формируем Python-скрипт
                StringBuilder scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("# -*- coding: utf-8 -*-");
                scriptBuilder.AppendLine("from reportlab.lib.pagesizes import A4");
                scriptBuilder.AppendLine("from reportlab.pdfgen import canvas");
                scriptBuilder.AppendLine("from reportlab.pdfbase import pdfmetrics");
                scriptBuilder.AppendLine("from reportlab.pdfbase.ttfonts import TTFont");
                scriptBuilder.AppendLine("from reportlab.lib.units import cm");
                scriptBuilder.AppendLine("from datetime import datetime");
                scriptBuilder.AppendLine("import os");
                scriptBuilder.AppendLine();

                // путь к PDF (используем os.path.join для кроссплатформенности)
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = string.Format("Отчёт_Пациенты_{0:yyyyMMdd_HHmmss}.pdf", DateTime.Now);

                scriptBuilder.AppendLine("import os");
                scriptBuilder.AppendFormat("documents_path = r\"\"\"{0}\"\"\"\n", documentsPath);
                scriptBuilder.AppendFormat("file_name = \"{0}\"\n", fileName);
                scriptBuilder.AppendLine("pdf_path = os.path.join(documents_path, file_name)");
                scriptBuilder.AppendLine();

                // регистрируем шрифт для кириллицы
                scriptBuilder.AppendLine("# Регистрация шрифта с поддержкой кириллицы");
                scriptBuilder.AppendLine("try:");
                scriptBuilder.AppendLine("    # Для Windows");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial', 'arial.ttf'))");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial-Bold', 'arialbd.ttf'))");
                scriptBuilder.AppendLine("    font_name = 'Arial'");
                scriptBuilder.AppendLine("    font_bold = 'Arial-Bold'");
                scriptBuilder.AppendLine("except:");
                scriptBuilder.AppendLine("    try:");
                scriptBuilder.AppendLine("        # Для Linux");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans', '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans-Bold', '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'))");
                scriptBuilder.AppendLine("        font_name = 'DejaVuSans'");
                scriptBuilder.AppendLine("        font_bold = 'DejaVuSans-Bold'");
                scriptBuilder.AppendLine("    except:");
                scriptBuilder.AppendLine("        font_name = 'Helvetica'");
                scriptBuilder.AppendLine("        font_bold = 'Helvetica-Bold'");
                scriptBuilder.AppendLine();

                // создаём PDF
                scriptBuilder.AppendLine("c = canvas.Canvas(pdf_path, pagesize=A4)");
                scriptBuilder.AppendLine("width, height = A4");
                scriptBuilder.AppendLine();

                // заголовок
                scriptBuilder.AppendLine("c.setFont(font_bold, 16)");
                scriptBuilder.AppendLine("c.drawCentredString(width/2, height - 2*cm, 'ОТЧЁТ О ТЕКУЩИХ ПАЦИЕНТАХ')");
                scriptBuilder.AppendLine();

                // дата формирования
                scriptBuilder.AppendLine("c.setFont(font_name, 9)");
                scriptBuilder.AppendFormat("c.drawString(2*cm, height - 3*cm, 'Дата формирования: {0:dd.MM.yyyy HH:mm}')\n",
                    DateTime.Now);
                scriptBuilder.AppendLine();

                // статистика
                scriptBuilder.AppendLine("c.setFont(font_bold, 11)");
                scriptBuilder.AppendFormat("c.drawString(2*cm, height - 4*cm, 'Текущих пациентов: {0}')\n",
                    stats.CurrentPatients);
                scriptBuilder.AppendLine();

                // заголовки таблицы
                scriptBuilder.AppendLine("y = height - 5*cm");
                scriptBuilder.AppendLine("c.setFont(font_bold, 9)");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, '№ Карты')");
                scriptBuilder.AppendLine("c.drawString(4.5*cm, y, 'ФИО')");
                scriptBuilder.AppendLine("c.drawString(9*cm, y, 'Возр.')");
                scriptBuilder.AppendLine("c.drawString(10.5*cm, y, 'Диагноз')");
                scriptBuilder.AppendLine("c.drawString(15*cm, y, 'Поступил')");
                scriptBuilder.AppendLine("c.drawString(17.5*cm, y, 'Дней')");
                scriptBuilder.AppendLine();

                // линия под заголовком
                scriptBuilder.AppendLine("y -= 0.3*cm");
                scriptBuilder.AppendLine("c.line(2*cm, y, width - 2*cm, y)");
                scriptBuilder.AppendLine();

                // данные пациентов
                scriptBuilder.AppendLine("c.setFont(font_name, 8)");
                scriptBuilder.AppendLine("y -= 0.5*cm");

                foreach (var patient in patients)
                {
                    // экранируем кавычки и спецсимволы в строках
                    string fullName = EscapeForPython(patient.FullName);
                    string diagnosis = EscapeForPython(patient.Diagnosis);

                    scriptBuilder.AppendLine("if y < 3*cm:");
                    scriptBuilder.AppendLine("    c.showPage()");
                    scriptBuilder.AppendLine("    c.setFont(font_name, 8)");
                    scriptBuilder.AppendLine("    y = height - 2*cm");
                    scriptBuilder.AppendLine();

                    scriptBuilder.AppendFormat("c.drawString(2*cm, y, '{0}')\n", patient.CardNumber);
                    scriptBuilder.AppendFormat("c.drawString(4.5*cm, y, '{0}')\n", fullName);
                    scriptBuilder.AppendFormat("c.drawString(9*cm, y, '{0}')\n", patient.Age);

                    // обрезаем длинные диагнозы
                    string shortDiagnosis = diagnosis.Length > 40 ? diagnosis.Substring(0, 37) + "..." : diagnosis;
                    scriptBuilder.AppendFormat("c.drawString(10.5*cm, y, '{0}')\n", shortDiagnosis);

                    scriptBuilder.AppendFormat("c.drawString(15*cm, y, '{0}')\n", patient.AdmissionDateDisplay);
                    scriptBuilder.AppendFormat("c.drawString(17.5*cm, y, '{0}')\n", patient.DaysInHospital);
                    scriptBuilder.AppendLine("y -= 0.5*cm");
                    scriptBuilder.AppendLine();
                }

                // сохраняем PDF
                scriptBuilder.AppendLine("c.save()");
                scriptBuilder.AppendLine("print('SUCCESS:' + pdf_path)");

                // записываем скрипт
                File.WriteAllText(scriptPath, scriptBuilder.ToString(), Encoding.UTF8);

                // запускаем Python
                string output = RunPythonScript(scriptPath);

                // удаляем временный скрипт
                try { File.Delete(scriptPath); } catch { }

                // проверяем, что файл создан
                if (File.Exists(pdfPath))
                {
                    return pdfPath;
                }
                else
                {
                    throw new Exception("PDF-файл не был создан");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка создания PDF-отчёта:\n{0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// генерирует PDF-отчёт о загрузке палат
        /// </summary>
        public string GenerateWardOccupancyReport()
        {
            try
            {
                var wards = reportService.GetWardOccupancyReport();

                string scriptPath = Path.Combine(Path.GetTempPath(), "generate_wards_report.py");
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = string.Format("Отчёт_Палаты_{0:yyyyMMdd_HHmmss}.pdf", DateTime.Now);
                string pdfPath = Path.Combine(documentsPath, fileName);

                StringBuilder scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("# -*- coding: utf-8 -*-");
                scriptBuilder.AppendLine("from reportlab.lib.pagesizes import A4");
                scriptBuilder.AppendLine("from reportlab.pdfgen import canvas");
                scriptBuilder.AppendLine("from reportlab.pdfbase import pdfmetrics");
                scriptBuilder.AppendLine("from reportlab.pdfbase.ttfonts import TTFont");
                scriptBuilder.AppendLine("from reportlab.lib.units import cm");
                scriptBuilder.AppendLine("import os");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendFormat("documents_path = r\"\"\"{0}\"\"\"\n", documentsPath);
                scriptBuilder.AppendFormat("file_name = \"{0}\"\n", fileName);
                scriptBuilder.AppendLine("pdf_path = os.path.join(documents_path, file_name)");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("try:");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial', 'arial.ttf'))");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial-Bold', 'arialbd.ttf'))");
                scriptBuilder.AppendLine("    font_name = 'Arial'");
                scriptBuilder.AppendLine("    font_bold = 'Arial-Bold'");
                scriptBuilder.AppendLine("except:");
                scriptBuilder.AppendLine("    try:");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans', '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans-Bold', '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'))");
                scriptBuilder.AppendLine("        font_name = 'DejaVuSans'");
                scriptBuilder.AppendLine("        font_bold = 'DejaVuSans-Bold'");
                scriptBuilder.AppendLine("    except:");
                scriptBuilder.AppendLine("        font_name = 'Helvetica'");
                scriptBuilder.AppendLine("        font_bold = 'Helvetica-Bold'");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("c = canvas.Canvas(pdf_path, pagesize=A4)");
                scriptBuilder.AppendLine("width, height = A4");
                scriptBuilder.AppendLine();

                // заголовок
                scriptBuilder.AppendLine("c.setFont(font_bold, 16)");
                scriptBuilder.AppendLine("c.drawCentredString(width/2, height - 2*cm, 'ОТЧЁТ О ЗАГРУЗКЕ ПАЛАТ')");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("c.setFont(font_name, 9)");
                scriptBuilder.AppendFormat("c.drawString(2*cm, height - 3*cm, 'Дата формирования: {0:dd.MM.yyyy HH:mm}')\n",
                    DateTime.Now);
                scriptBuilder.AppendLine();

                // заголовки таблицы
                scriptBuilder.AppendLine("y = height - 4.5*cm");
                scriptBuilder.AppendLine("c.setFont(font_bold, 9)");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, 'Отделение')");
                scriptBuilder.AppendLine("c.drawString(7*cm, y, 'Палата')");
                scriptBuilder.AppendLine("c.drawString(9*cm, y, 'Всего')");
                scriptBuilder.AppendLine("c.drawString(11*cm, y, 'Занято')");
                scriptBuilder.AppendLine("c.drawString(13*cm, y, 'Свободно')");
                scriptBuilder.AppendLine("c.drawString(15.5*cm, y, 'Загрузка')");
                scriptBuilder.AppendLine("c.drawString(18*cm, y, 'Статус')");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("y -= 0.3*cm");
                scriptBuilder.AppendLine("c.line(2*cm, y, width - 2*cm, y)");
                scriptBuilder.AppendLine();

                // данные палат
                scriptBuilder.AppendLine("c.setFont(font_name, 8)");
                scriptBuilder.AppendLine("y -= 0.5*cm");

                foreach (var ward in wards)
                {
                    string department = EscapeForPython(ward.Department);
                    string statusText = EscapeForPython(ward.StatusText);

                    scriptBuilder.AppendLine("if y < 3*cm:");
                    scriptBuilder.AppendLine("    c.showPage()");
                    scriptBuilder.AppendLine("    c.setFont(font_name, 8)");
                    scriptBuilder.AppendLine("    y = height - 2*cm");
                    scriptBuilder.AppendLine();

                    scriptBuilder.AppendFormat("c.drawString(2*cm, y, '{0}')\n", department);
                    scriptBuilder.AppendFormat("c.drawString(7*cm, y, '{0}')\n", ward.WardNumber);
                    scriptBuilder.AppendFormat("c.drawString(9*cm, y, '{0}')\n", ward.TotalBeds);
                    scriptBuilder.AppendFormat("c.drawString(11*cm, y, '{0}')\n", ward.OccupiedBeds);
                    scriptBuilder.AppendFormat("c.drawString(13*cm, y, '{0}')\n", ward.AvailableBeds);
                    scriptBuilder.AppendFormat("c.drawString(15.5*cm, y, '{0:F1}%')\n", ward.OccupancyRate);
                    scriptBuilder.AppendFormat("c.drawString(18*cm, y, '{0}')\n", statusText);
                    scriptBuilder.AppendLine("y -= 0.5*cm");
                    scriptBuilder.AppendLine();
                }

                // легенда
                scriptBuilder.AppendLine("if y < 4*cm:");
                scriptBuilder.AppendLine("    c.showPage()");
                scriptBuilder.AppendLine("    y = height - 2*cm");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("y -= 1*cm");
                scriptBuilder.AppendLine("c.setFont(font_bold, 9)");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, 'Легенда:')");
                scriptBuilder.AppendLine("y -= 0.5*cm");
                scriptBuilder.AppendLine("c.setFont(font_name, 8)");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, '• Есть места: загрузка < 50%')");
                scriptBuilder.AppendLine("y -= 0.4*cm");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, '• Заполнена наполовину: 50-79%')");
                scriptBuilder.AppendLine("y -= 0.4*cm");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, '• Почти полная: >= 80%')");
                scriptBuilder.AppendLine("y -= 0.4*cm");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, '• Полностью занята: 100%')");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("c.save()");
                scriptBuilder.AppendLine("print('SUCCESS:' + pdf_path)");

                File.WriteAllText(scriptPath, scriptBuilder.ToString(), Encoding.UTF8);

                string output = RunPythonScript(scriptPath);

                try { File.Delete(scriptPath); } catch { }

                if (File.Exists(pdfPath))
                {
                    return pdfPath;
                }
                else
                {
                    throw new Exception("PDF-файл не был создан");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка создания PDF-отчёта:\n{0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// генерирует PDF-отчёт по статистике диагнозов
        /// </summary>
        public string GenerateDiagnosisStatisticsReport(DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                var statistics = reportService.GetDiagnosisStatistics(dateFrom, dateTo);

                string scriptPath = Path.Combine(Path.GetTempPath(), "generate_diagnosis_report.py");
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string fileName = string.Format("Отчёт_Диагнозы_{0:yyyyMMdd_HHmmss}.pdf", DateTime.Now);
                string pdfPath = Path.Combine(documentsPath, fileName);

                StringBuilder scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("# -*- coding: utf-8 -*-");
                scriptBuilder.AppendLine("from reportlab.lib.pagesizes import A4");
                scriptBuilder.AppendLine("from reportlab.pdfgen import canvas");
                scriptBuilder.AppendLine("from reportlab.pdfbase import pdfmetrics");
                scriptBuilder.AppendLine("from reportlab.pdfbase.ttfonts import TTFont");
                scriptBuilder.AppendLine("from reportlab.lib.units import cm");
                scriptBuilder.AppendLine("import os");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendFormat("documents_path = r\"\"\"{0}\"\"\"\n", documentsPath);
                scriptBuilder.AppendFormat("file_name = \"{0}\"\n", fileName);
                scriptBuilder.AppendLine("pdf_path = os.path.join(documents_path, file_name)");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("try:");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial', 'arial.ttf'))");
                scriptBuilder.AppendLine("    pdfmetrics.registerFont(TTFont('Arial-Bold', 'arialbd.ttf'))");
                scriptBuilder.AppendLine("    font_name = 'Arial'");
                scriptBuilder.AppendLine("    font_bold = 'Arial-Bold'");
                scriptBuilder.AppendLine("except:");
                scriptBuilder.AppendLine("    try:");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans', '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))");
                scriptBuilder.AppendLine("        pdfmetrics.registerFont(TTFont('DejaVuSans-Bold', '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'))");
                scriptBuilder.AppendLine("        font_name = 'DejaVuSans'");
                scriptBuilder.AppendLine("        font_bold = 'DejaVuSans-Bold'");
                scriptBuilder.AppendLine("    except:");
                scriptBuilder.AppendLine("        font_name = 'Helvetica'");
                scriptBuilder.AppendLine("        font_bold = 'Helvetica-Bold'");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("c = canvas.Canvas(pdf_path, pagesize=A4)");
                scriptBuilder.AppendLine("width, height = A4");
                scriptBuilder.AppendLine();

                // заголовок
                scriptBuilder.AppendLine("c.setFont(font_bold, 16)");
                scriptBuilder.AppendLine("c.drawCentredString(width/2, height - 2*cm, 'СТАТИСТИКА ПО ДИАГНОЗАМ')");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("c.setFont(font_name, 9)");
                scriptBuilder.AppendFormat("c.drawString(2*cm, height - 3*cm, 'Дата формирования: {0:dd.MM.yyyy HH:mm}')\n",
                    DateTime.Now);

                // период
                if (dateFrom.HasValue || dateTo.HasValue)
                {
                    string period = "";
                    if (dateFrom.HasValue && dateTo.HasValue)
                        period = string.Format("Период: {0:dd.MM.yyyy} - {1:dd.MM.yyyy}",
                            dateFrom.Value, dateTo.Value);
                    else if (dateFrom.HasValue)
                        period = string.Format("Период: с {0:dd.MM.yyyy}", dateFrom.Value);
                    else
                        period = string.Format("Период: по {0:dd.MM.yyyy}", dateTo.Value);

                    scriptBuilder.AppendFormat("c.drawString(2*cm, height - 3.5*cm, '{0}')\n", period);
                }

                scriptBuilder.AppendLine();

                // заголовки таблицы
                scriptBuilder.AppendLine("y = height - 5*cm");
                scriptBuilder.AppendLine("c.setFont(font_bold, 9)");
                scriptBuilder.AppendLine("c.drawString(2*cm, y, 'Диагноз')");
                scriptBuilder.AppendLine("c.drawString(13*cm, y, 'Пациентов')");
                scriptBuilder.AppendLine("c.drawString(16*cm, y, 'Средн. длительность')");
                scriptBuilder.AppendLine();

                scriptBuilder.AppendLine("y -= 0.3*cm");
                scriptBuilder.AppendLine("c.line(2*cm, y, width - 2*cm, y)");
                scriptBuilder.AppendLine();

                // данные
                scriptBuilder.AppendLine("c.setFont(font_name, 8)");
                scriptBuilder.AppendLine("y -= 0.5*cm");

                foreach (var diag in statistics)
                {
                    string diagnosis = EscapeForPython(diag.Diagnosis);
                    string shortDiagnosis = diagnosis.Length > 80 ? diagnosis.Substring(0, 77) + "..." : diagnosis;

                    scriptBuilder.AppendLine("if y < 3*cm:");
                    scriptBuilder.AppendLine("    c.showPage()");
                    scriptBuilder.AppendLine("    c.setFont(font_name, 8)");
                    scriptBuilder.AppendLine("    y = height - 2*cm");
                    scriptBuilder.AppendLine();

                    scriptBuilder.AppendFormat("c.drawString(2*cm, y, '{0}')\n", shortDiagnosis);
                    scriptBuilder.AppendFormat("c.drawString(13*cm, y, '{0}')\n", diag.PatientCount);
                    scriptBuilder.AppendFormat("c.drawString(16*cm, y, '{0} дн.')\n", diag.AverageDuration);
                    scriptBuilder.AppendLine("y -= 0.5*cm");
                    scriptBuilder.AppendLine();
                }

                scriptBuilder.AppendLine("c.save()");
                scriptBuilder.AppendLine("print('SUCCESS:' + pdf_path)");

                File.WriteAllText(scriptPath, scriptBuilder.ToString(), Encoding.UTF8);

                string output = RunPythonScript(scriptPath);

                try { File.Delete(scriptPath); } catch { }

                if (File.Exists(pdfPath))
                {
                    return pdfPath;
                }
                else
                {
                    throw new Exception("PDF-файл не был создан");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка создания PDF-отчёта:\n{0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// запускает Python-скрипт и возвращает вывод
        /// </summary>
        private string RunPythonScript(string scriptPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python",  // или "python3" на Linux
                Arguments = string.Format("\"{0}\"", scriptPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception(string.Format("Ошибка генерации PDF:\n{0}", error));
                }

                return output;
            }
        }

        /// <summary>
        /// экранирует строку для безопасного использования в Python
        /// </summary>
        private string EscapeForPython(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text
                .Replace("\\", "\\\\")  // обратный слэш
                .Replace("'", "\\'")     // одинарная кавычка
                .Replace("\"", "\\\"")   // двойная кавычка
                .Replace("\n", "\\n")    // перевод строки
                .Replace("\r", "");      // возврат каретки (убираем)
        }
    }
}