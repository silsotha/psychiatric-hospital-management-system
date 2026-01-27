using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Services
{
    public class ReportService
    {
        /// <summary>
        /// отчёт о текущих пациентах
        /// </summary>
        public List<CurrentPatientReport> GetCurrentPatientsReport()
        {
            var report = new List<CurrentPatientReport>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT
                            p.PatientId,
                            p.CardNumber,
                            p.FullName,
                            p.BirthDate,
                            p.Diagnosis,
                            p.AdmissionDate,
                            w.WardNumber,
                            w.Department
                        FROM Patients p
                        LEFT JOIN Wards w ON p.WardId = w.WardId
                        WHERE p.Status = 'active'
                        ORDER BY p.AdmissionDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int age = DateTime.Now.Year - ((DateTime)reader["BirthDate"]).Year;
                            if (DateTime.Now.DayOfYear < ((DateTime)reader["BirthDate"]).DayOfYear)
                                age--;

                            int daysInHospital = (DateTime.Now - (DateTime)reader["AdmissionDate"]).Days;

                            report.Add(new CurrentPatientReport
                            {
                                CardNumber = reader["CardNumber"].ToString(),
                                FullName = reader["FullName"].ToString(),
                                Age = age,
                                Diagnosis = reader["Diagnosis"] != DBNull.Value
                                    ? reader["Diagnosis"].ToString() : "-",
                                AdmissionDate = (DateTime)reader["AdmissionDate"],
                                WardNumber = reader["WardNumber"] != DBNull.Value
                                    ? reader["WardNumber"].ToString() : "Не назначена",
                                Department = reader["Department"] != DBNull.Value
                                    ? reader["Department"].ToString() : "-",
                                DaysInHospital = daysInHospital
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка формирования отчёта: {0}", ex.Message), ex);
            }

            return report;
        }

        /// <summary>
        /// отчёт о загрузке палат и отделений
        /// </summary>
        public List<WardOccupancyReport> GetWardOccupancyReport()
        {
            var report = new List<WardOccupancyReport>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            Department,
                            WardNumber,
                            TotalBeds,
                            OccupiedBeds
                        FROM Wards
                        ORDER BY Department, WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int totalBeds = (int)reader["TotalBeds"];
                            int occupiedBeds = (int)reader["OccupiedBeds"];

                            report.Add(new WardOccupancyReport
                            {
                                Department = reader["Department"].ToString(),
                                WardNumber = reader["WardNumber"].ToString(),
                                TotalBeds = totalBeds,
                                OccupiedBeds = occupiedBeds,
                                AvailableBeds = totalBeds - occupiedBeds,
                                OccupancyRate = totalBeds > 0
                                    ? (double)occupiedBeds / totalBeds * 100
                                    : 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка формирования отчёта: {0}", ex.Message), ex);
            }

            return report;
        }

        /// <summary>
        /// статистика по диагнозам за период
        /// </summary>
        public List<DiagnosisStatistics> GetDiagnosisStatistics(DateTime? dateFrom, DateTime? dateTo)
        {
            var statistics = new List<DiagnosisStatistics>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            COALESCE(Diagnosis, 'Не указан') as Diagnosis,
                            COUNT(*) as PatientCount,
                            AVG(DATEDIFF(day, AdmissionDate, COALESCE(DischargeDate, GETDATE()))) as AvgDuration
                        FROM Patients
                        WHERE 1=1";

                    if (dateFrom.HasValue)
                        query += " AND AdmissionDate >= @DateFrom";

                    if (dateTo.HasValue)
                        query += " AND AdmissionDate <= @DateTo";

                    query += @"
                        GROUP BY COALESCE(Diagnosis, 'Не указан')
                        ORDER BY PatientCount DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (dateFrom.HasValue)
                            cmd.Parameters.AddWithValue("@DateFrom", dateFrom.Value);

                        if (dateTo.HasValue)
                            cmd.Parameters.AddWithValue("@DateTo", dateTo.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                statistics.Add(new DiagnosisStatistics
                                {
                                    Diagnosis = reader["Diagnosis"].ToString(),
                                    PatientCount = (int)reader["PatientCount"],
                                    AverageDuration = reader["AvgDuration"] != DBNull.Value
                                        ? (int)reader["AvgDuration"]
                                        : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка формирования статистики: {0}", ex.Message), ex);
            }

            return statistics;
        }

        /// <summary>
        /// общая статистика больницы
        /// </summary>
        public HospitalStatistics GetHospitalStatistics()
        {
            var stats = new HospitalStatistics();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // текущие пациенты
                    string query1 = "SELECT COUNT(*) FROM Patients WHERE Status = 'active'";
                    using (var cmd = new SqlCommand(query1, conn))
                    {
                        stats.CurrentPatients = (int)cmd.ExecuteScalar();
                    }

                    // всего выписано
                    string query2 = "SELECT COUNT(*) FROM Patients WHERE Status = 'discharged'";
                    using (var cmd = new SqlCommand(query2, conn))
                    {
                        stats.TotalDischarged = (int)cmd.ExecuteScalar();
                    }

                    // всего палат и мест
                    string query3 = "SELECT SUM(TotalBeds), SUM(OccupiedBeds) FROM Wards";
                    using (var cmd = new SqlCommand(query3, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            stats.TotalBeds = reader[0] != DBNull.Value ? (int)reader[0] : 0;
                            stats.OccupiedBeds = reader[1] != DBNull.Value ? (int)reader[1] : 0;
                        }
                    }

                    // активные назначения
                    string query4 = "SELECT COUNT(*) FROM Prescriptions WHERE Status = N'Активно'";
                    using (var cmd = new SqlCommand(query4, conn))
                    {
                        stats.ActivePrescriptions = (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения статистики: {0}", ex.Message), ex);
            }

            return stats;
        }
    }

    /// <summary>
    /// отчёт о текущем пациенте
    /// </summary>
    public class CurrentPatientReport
    {
        public string CardNumber { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Diagnosis { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string WardNumber { get; set; }
        public string Department { get; set; }
        public int DaysInHospital { get; set; }

        public string AdmissionDateDisplay
        {
            get { return AdmissionDate.ToString("dd.MM.yyyy"); }
        }

        public string DaysInHospitalDisplay
        {
            get { return string.Format("{0} дн.", DaysInHospital); }
        }
    }

    /// <summary>
    /// отчёт о загрузке палаты
    /// </summary>
    public class WardOccupancyReport
    {
        public string Department { get; set; }
        public string WardNumber { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyRate { get; set; }

        public string OccupancyDisplay
        {
            get { return string.Format("{0:F1}%", OccupancyRate); }
        }

        /// <summary>
        /// цвет статуса в виде строки (для XAML Binding)
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (AvailableBeds == 0)
                    return "#f44336"; // красный - полностью занята
                else if (OccupancyRate >= 80)
                    return "#FF9800"; // оранжевый - почти полная (80-99%)
                else if (OccupancyRate >= 50)
                    return "#FFC107"; // жёлтый - заполнена наполовину (50-79%)
                else
                    return "#4CAF50"; // зелёный - есть места (< 50%)
            }
        }

        /// <summary>
        /// brush для UI
        /// </summary>
        public System.Windows.Media.SolidColorBrush StatusBrush
        {
            get
            {
                var converter = new System.Windows.Media.BrushConverter();
                return (System.Windows.Media.SolidColorBrush)converter.ConvertFromString(StatusColor);
            }
        }

        /// <summary>
        /// текстовое описание статуса
        /// </summary>
        public string StatusText
        {
            get
            {
                if (AvailableBeds == 0)
                    return "Полностью занята";
                else if (OccupancyRate >= 80)
                    return "Почти полная";
                else if (OccupancyRate >= 50)
                    return "Заполнена наполовину";
                else
                    return "Есть места";
            }
        }
    }

    /// <summary>
    /// статистика по диагнозу
    /// </summary>
    public class DiagnosisStatistics
    {
        public string Diagnosis { get; set; }
        public int PatientCount { get; set; }
        public int AverageDuration { get; set; }

        public string AverageDurationDisplay
        {
            get { return string.Format("{0} дн.", AverageDuration); }
        }
    }

    /// <summary>
    /// общая статистика больницы
    /// </summary>
    public class HospitalStatistics
    {
        public int CurrentPatients { get; set; }
        public int TotalDischarged { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int ActivePrescriptions { get; set; }

        public int AvailableBeds
        {
            get { return TotalBeds - OccupiedBeds; }
        }

        public double OccupancyRate
        {
            get
            {
                return TotalBeds > 0
                    ? (double)OccupiedBeds / TotalBeds * 100
                    : 0;
            }
        }

        public string OccupancyRateDisplay
        {
            get { return string.Format("{0:F1}%", OccupancyRate); }
        }
    }
}