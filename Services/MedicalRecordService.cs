using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Services
{
    public class MedicalRecordService
    {
        private readonly AuthService authService = new AuthService();


        // получить все медицинские записи пациента
        public List<MedicalRecord> GetRecordsByPatient(int patientId)
        {
            var records = new List<MedicalRecord>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT mr.*, u.FullName as DoctorName, p.FullName as PatientName
                        FROM MedicalRecords mr
                        INNER JOIN Users u ON mr.DoctorId = u.UserId
                        INNER JOIN Patients p ON mr.PatientId = p.PatientId
                        WHERE mr.PatientId = @PatientId
                        ORDER BY mr.RecordDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(MapRecord(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка получения записей: {0}", ex.Message), ex);
            }

            return records;
        }

        // добавить новую медицинскую запись
        public int AddRecord(MedicalRecord record)
        {
            if (!UserSession.CanEditMedicalRecords())
            {
                throw new UnauthorizedAccessException(
                    "Недостаточно прав для добавления медицинских записей");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO MedicalRecords 
                        (PatientId, DoctorId, RecordDate, Description, RecordType)
                        OUTPUT INSERTED.RecordId
                        VALUES 
                        (@PatientId, @DoctorId, @RecordDate, @Description, @RecordType)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", record.PatientId);
                        cmd.Parameters.AddWithValue("@DoctorId",
                            UserSession.CurrentUser.UserId);
                        cmd.Parameters.AddWithValue("@RecordDate", record.RecordDate);
                        cmd.Parameters.AddWithValue("@Description", record.Description);
                        cmd.Parameters.AddWithValue("@RecordType",
                            record.RecordType ?? (object)DBNull.Value);

                        int recordId = (int)cmd.ExecuteScalar();

                        // Логируем действие
                        authService.AddAuditLog(
                            UserSession.CurrentUser.UserId,
                            "ADD_MEDICAL_RECORD",
                            "MedicalRecord",
                            recordId,
                            string.Format("Добавлена запись ({0}) для пациента ID:{1}",
                                record.RecordType ?? "без типа", record.PatientId));

                        return recordId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка добавления записи: {0}", ex.Message), ex);
            }
        }

        // получить запись по ID
        public MedicalRecord GetRecordById(int recordId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT mr.*, u.FullName as DoctorName, p.FullName as PatientName
                        FROM MedicalRecords mr
                        INNER JOIN Users u ON mr.DoctorId = u.UserId
                        INNER JOIN Patients p ON mr.PatientId = p.PatientId
                        WHERE mr.RecordId = @RecordId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RecordId", recordId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapRecord(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка получения записи: {0}", ex.Message), ex);
            }

            return null;
        }


        // поиск записей по описанию
        public List<MedicalRecord> SearchRecords(int patientId, string searchTerm)
        {
            var records = new List<MedicalRecord>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT mr.*, u.FullName as DoctorName, p.FullName as PatientName
                        FROM MedicalRecords mr
                        INNER JOIN Users u ON mr.DoctorId = u.UserId
                        INNER JOIN Patients p ON mr.PatientId = p.PatientId
                        WHERE mr.PatientId = @PatientId 
                          AND mr.Description LIKE @Search
                        ORDER BY mr.RecordDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);
                        cmd.Parameters.AddWithValue("@Search", string.Format("%{0}%", searchTerm));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(MapRecord(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка поиска: {0}", ex.Message), ex);
            }

            return records;
        }

        // фильтрация записей по типу и датам
        public List<MedicalRecord> FilterRecords(int patientId,
                                                 DateTime? dateFrom = null,
                                                 DateTime? dateTo = null,
                                                 string recordType = null)
        {
            var records = new List<MedicalRecord>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT mr.*, u.FullName as DoctorName, p.FullName as PatientName
                        FROM MedicalRecords mr
                        INNER JOIN Users u ON mr.DoctorId = u.UserId
                        INNER JOIN Patients p ON mr.PatientId = p.PatientId
                        WHERE mr.PatientId = @PatientId";

                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.Parameters.AddWithValue("@PatientId", patientId);

                        if (dateFrom.HasValue)
                        {
                            query += " AND CAST(mr.RecordDate AS DATE) >= @DateFrom";
                            cmd.Parameters.AddWithValue("@DateFrom", dateFrom.Value.Date);
                        }

                        if (dateTo.HasValue)
                        {
                            query += " AND CAST(mr.RecordDate AS DATE) <= @DateTo";
                            cmd.Parameters.AddWithValue("@DateTo", dateTo.Value.Date);
                        }

                        if (!string.IsNullOrEmpty(recordType))
                        {
                            query += " AND mr.RecordType = @RecordType";
                            cmd.Parameters.AddWithValue("@RecordType", recordType);
                        }

                        query += " ORDER BY mr.RecordDate DESC";
                        cmd.CommandText = query;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(MapRecord(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка фильтрации: {0}", ex.Message), ex);
            }

            return records;
        }

        // маппинг из SqlDataReader в MedicalRecord
        private MedicalRecord MapRecord(SqlDataReader reader)
        {
            return new MedicalRecord
            {
                RecordId = (int)reader["RecordId"],
                PatientId = (int)reader["PatientId"],
                DoctorId = (int)reader["DoctorId"],
                RecordDate = (DateTime)reader["RecordDate"],
                Description = reader["Description"].ToString(),
                RecordType = reader["RecordType"] != DBNull.Value
                    ? reader["RecordType"].ToString()
                    : null,
                CreatedAt = (DateTime)reader["CreatedAt"],
                DoctorName = reader["DoctorName"].ToString(),
                PatientName = reader["PatientName"].ToString()
            };
        }
    }
}