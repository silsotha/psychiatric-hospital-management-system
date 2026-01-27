using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Services
{
    public class WardService
    {
        private readonly AuthService authService = new AuthService();

        /// <summary>
        /// получить все палаты
        /// </summary>
        public List<Ward> GetAllWards()
        {
            var wards = new List<Ward>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT WardId, WardNumber, Department, TotalBeds, OccupiedBeds
                        FROM Wards
                        ORDER BY WardNumber";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            wards.Add(MapWard(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения списка палат: {0}", ex.Message), ex);
            }

            return wards;
        }

        /// <summary>
        /// получить палату по ID
        /// </summary>
        public Ward GetWardById(int wardId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT WardId, WardNumber, Department, TotalBeds, OccupiedBeds
                        FROM Wards
                        WHERE WardId = @WardId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WardId", wardId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapWard(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения палаты: {0}", ex.Message), ex);
            }

            return null;
        }

        /// <summary>
        /// получить пациентов в палате
        /// </summary>
        public List<Patient> GetPatientsByWard(int wardId)
        {
            var patients = new List<Patient>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT p.*, w.WardNumber
                        FROM Patients p
                        INNER JOIN Wards w ON p.WardId = w.WardId
                        WHERE p.WardId = @WardId AND p.Status = 'active'
                        ORDER BY p.AdmissionDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WardId", wardId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                patients.Add(MapPatient(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения пациентов палаты: {0}", ex.Message), ex);
            }

            return patients;
        }

        /// <summary>
        /// перевести пациента в другую палату
        /// </summary>
        public void TransferPatient(int patientId, int oldWardId, int newWardId, string reason)
        {
            if (oldWardId == newWardId)
            {
                throw new ArgumentException("Пациент уже находится в этой палате");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // проверяем наличие свободных мест
                            string checkQuery = @"
                                SELECT TotalBeds, OccupiedBeds 
                                FROM Wards 
                                WHERE WardId = @WardId";

                            using (var cmd = new SqlCommand(checkQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@WardId", newWardId);

                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int totalBeds = (int)reader["TotalBeds"];
                                        int occupiedBeds = (int)reader["OccupiedBeds"];

                                        if (occupiedBeds >= totalBeds)
                                        {
                                            throw new InvalidOperationException(
                                                "В новой палате нет свободных мест");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Палата не найдена");
                                    }
                                }
                            }

                            // освобождаем место в старой палате
                            string updateOldQuery = @"
                                UPDATE Wards 
                                SET OccupiedBeds = OccupiedBeds - 1 
                                WHERE WardId = @WardId";

                            using (var cmd = new SqlCommand(updateOldQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@WardId", oldWardId);
                                cmd.ExecuteNonQuery();
                            }

                            // занимаем место в новой палате
                            string updateNewQuery = @"
                                UPDATE Wards 
                                SET OccupiedBeds = OccupiedBeds + 1 
                                WHERE WardId = @WardId";

                            using (var cmd = new SqlCommand(updateNewQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@WardId", newWardId);
                                cmd.ExecuteNonQuery();
                            }

                            // обновляем палату у пациента
                            string updatePatientQuery = @"
                                UPDATE Patients 
                                SET WardId = @NewWardId 
                                WHERE PatientId = @PatientId";

                            using (var cmd = new SqlCommand(updatePatientQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@NewWardId", newWardId);
                                cmd.Parameters.AddWithValue("@PatientId", patientId);
                                cmd.ExecuteNonQuery();
                            }

                            // логируем действие
                            if (UserSession.IsAuthenticated)
                            {
                                authService.AddAuditLog(
                                    UserSession.CurrentUser.UserId,
                                    "TRANSFER_PATIENT",
                                    "Patient",
                                    patientId,
                                    string.Format(
                                        "Перевод из палаты {0} в палату {1}. Причина: {2}",
                                        oldWardId, newWardId, reason ?? "Не указана"));
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка перевода пациента: {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// получить статистику по отделениям
        /// </summary>
        public List<DepartmentStats> GetDepartmentStats()
        {
            var stats = new List<DepartmentStats>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            Department,
                            COUNT(*) as WardCount,
                            SUM(TotalBeds) as TotalBeds,
                            SUM(OccupiedBeds) as OccupiedBeds
                        FROM Wards
                        GROUP BY Department
                        ORDER BY Department";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stats.Add(new DepartmentStats
                            {
                                Department = reader["Department"].ToString(),
                                WardCount = (int)reader["WardCount"],
                                TotalBeds = (int)reader["TotalBeds"],
                                OccupiedBeds = (int)reader["OccupiedBeds"]
                            });
                        }
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

        private Ward MapWard(SqlDataReader reader)
        {
            return new Ward
            {
                WardId = (int)reader["WardId"],
                WardNumber = reader["WardNumber"].ToString(),
                Department = reader["Department"].ToString(),
                TotalBeds = (int)reader["TotalBeds"],
                OccupiedBeds = (int)reader["OccupiedBeds"]
            };
        }

        private Patient MapPatient(SqlDataReader reader)
        {
            return new Patient
            {
                PatientId = (int)reader["PatientId"],
                CardNumber = reader["CardNumber"].ToString(),
                FullName = reader["FullName"].ToString(),
                BirthDate = (DateTime)reader["BirthDate"],
                ContactInfo = reader["ContactInfo"] != DBNull.Value
                    ? reader["ContactInfo"].ToString() : null,
                AdmissionDate = (DateTime)reader["AdmissionDate"],
                Diagnosis = reader["Diagnosis"] != DBNull.Value
                    ? reader["Diagnosis"].ToString() : null,
                Status = reader["Status"].ToString(),
                DischargeDate = reader["DischargeDate"] != DBNull.Value
                    ? (DateTime?)reader["DischargeDate"] : null,
                WardId = reader["WardId"] != DBNull.Value
                    ? (int?)reader["WardId"] : null,
                WardNumber = reader["WardNumber"] != DBNull.Value
                    ? reader["WardNumber"].ToString() : "-"
            };
        }
    }

    /// <summary>
    /// статистика по отделению
    /// </summary>
    public class DepartmentStats
    {
        public string Department { get; set; }
        public int WardCount { get; set; }
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }

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

        public string OccupancyDisplay
        {
            get { return string.Format("{0:F1}%", OccupancyRate); }
        }
    }
}