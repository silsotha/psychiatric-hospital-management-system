using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Services
{
    public class PatientService
    {
        private readonly AuthService authService = new AuthService();

        public List<Patient> GetAllPatients(string status = null)
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
                        LEFT JOIN Wards w ON p.WardId = w.WardId";

                    if (!string.IsNullOrEmpty(status))
                        query += " WHERE p.Status = @Status";

                    query += " ORDER BY p.AdmissionDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(status))
                            cmd.Parameters.AddWithValue("@Status", status);

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
                throw new Exception(string.Format("Ошибка при получении списка пациентов: {0}", ex.Message), ex);
            }

            return patients;
        }

        public Patient GetPatientById(int patientId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT p.*, w.WardNumber
                        FROM Patients p
                        LEFT JOIN Wards w ON p.WardId = w.WardId
                        WHERE p.PatientId = @PatientId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapPatient(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка при получении пациента: {0}", ex.Message), ex);
            }

            return null;
        }

        // поиск пациентов по ФИО или номеру карты (обновленная версия)
        public List<Patient> SearchPatients(string searchTerm)
        {
            var patients = new List<Patient>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // нормализуем поисковый запрос
                    string normalizedTerm = searchTerm.Trim();

                    // заменяем кириллические буквы на латинские для поиска по номеру карты
                    string latinTerm = normalizedTerm
                        .Replace("М", "M")
                        .Replace("м", "M")
                        .Replace("К", "K")
                        .Replace("к", "K");

                    string cyrillicTerm = latinTerm
                        .Replace("M", "М")
                        .Replace("K", "К");

                    string query = @"
                        SELECT p.*, w.WardNumber
                        FROM Patients p
                        LEFT JOIN Wards w ON p.WardId = w.WardId
                        WHERE p.FullName LIKE @Search 
                           OR p.CardNumber LIKE @SearchLatin
                           OR p.CardNumber LIKE @SearchCyrillic
                        ORDER BY p.AdmissionDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", string.Format("%{0}%", searchTerm));
                        cmd.Parameters.AddWithValue("@SearchLatin", string.Format("%{0}%", latinTerm));
                        cmd.Parameters.AddWithValue("@SearchCyrillic", string.Format("%{0}%", cyrillicTerm));

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
                throw new Exception(string.Format("Ошибка поиска: {0}", ex.Message), ex);
            }

            return patients;
        }


        /// расширенный поиск пациентов по критериям
        public List<Patient> AdvancedSearch(string fullName = null,
                                            string cardNumber = null,
                                            string wardNumber = null,
                                            string status = null)
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
                        LEFT JOIN Wards w ON p.WardId = w.WardId
                        WHERE 1=1";

                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;

                        // фильтр по ФИО (частичное совпадение)
                        if (!string.IsNullOrEmpty(fullName))
                        {
                            query += " AND p.FullName LIKE @FullName";
                            cmd.Parameters.AddWithValue("@FullName", string.Format("%{0}%", fullName));
                        }

                        // фильтр по номеру карты
                        // поддержка кириллицы (МК) и латиницы (MK)
                        if (!string.IsNullOrEmpty(cardNumber))
                        {
                            // нормализуем ввод пользователя
                            string normalizedInput = cardNumber.Trim();

                            // заменяем кириллические буквы на латинские
                            normalizedInput = normalizedInput
                                .Replace("М", "M")
                                .Replace("м", "M")
                                .Replace("К", "K")
                                .Replace("к", "K");

                            // ищем по обоим вариантам в бд на всякий случай и заменяем
                            string latinPattern = string.Format("%{0}%", normalizedInput);
                            string cyrillicPattern = latinPattern
                                .Replace("M", "М")
                                .Replace("K", "К");

                            query += " AND (p.CardNumber LIKE @CardNumberLatin OR p.CardNumber LIKE @CardNumberCyrillic)";
                            cmd.Parameters.AddWithValue("@CardNumberLatin", latinPattern);
                            cmd.Parameters.AddWithValue("@CardNumberCyrillic", cyrillicPattern);
                        }

                        // фильтр по палате
                        if (!string.IsNullOrEmpty(wardNumber))
                        {
                            query += " AND w.WardNumber = @WardNumber";
                            cmd.Parameters.AddWithValue("@WardNumber", wardNumber);
                        }

                        // фильтр по статусу
                        if (!string.IsNullOrEmpty(status))
                        {
                            query += " AND p.Status = @Status";
                            cmd.Parameters.AddWithValue("@Status", status);
                        }

                        query += " ORDER BY p.AdmissionDate DESC";

                        cmd.CommandText = query;

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
                throw new Exception(string.Format("Ошибка расширенного поиска: {0}", ex.Message), ex);
            }

            return patients;
        }

        public int CreatePatient(Patient patient)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    patient.CardNumber = GenerateCardNumber(conn);

                    string query = @"
                        INSERT INTO Patients 
                        (CardNumber, FullName, BirthDate, ContactInfo, 
                         AdmissionDate, Diagnosis, Status, WardId)
                        OUTPUT INSERTED.PatientId
                        VALUES 
                        (@CardNumber, @FullName, @BirthDate, @ContactInfo, 
                         @AdmissionDate, @Diagnosis, @Status, @WardId)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CardNumber", patient.CardNumber);
                        cmd.Parameters.AddWithValue("@FullName", patient.FullName);
                        cmd.Parameters.AddWithValue("@BirthDate", patient.BirthDate);
                        cmd.Parameters.AddWithValue("@ContactInfo",
                            patient.ContactInfo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AdmissionDate", patient.AdmissionDate);
                        cmd.Parameters.AddWithValue("@Diagnosis",
                            patient.Diagnosis ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Status", "active");
                        cmd.Parameters.AddWithValue("@WardId",
                            patient.WardId ?? (object)DBNull.Value);

                        int patientId = (int)cmd.ExecuteScalar();

                        if (patient.WardId.HasValue)
                            UpdateWardOccupancy(patient.WardId.Value, 1, conn);

                        if (UserSession.IsAuthenticated)
                        {
                            authService.AddAuditLog(
                                UserSession.CurrentUser.UserId,
                                "CREATE_PATIENT",
                                "Patient",
                                patientId,
                                string.Format("Создан пациент: {0}", patient.FullName));
                        }

                        return patientId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка при создании пациента: {0}", ex.Message), ex);
            }
        }

        public void UpdatePatient(Patient patient)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    var oldPatient = GetPatientById(patient.PatientId);

                    string query = @"
                        UPDATE Patients SET
                            FullName = @FullName,
                            BirthDate = @BirthDate,
                            ContactInfo = @ContactInfo,
                            Diagnosis = @Diagnosis,
                            WardId = @WardId
                        WHERE PatientId = @PatientId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patient.PatientId);
                        cmd.Parameters.AddWithValue("@FullName", patient.FullName);
                        cmd.Parameters.AddWithValue("@BirthDate", patient.BirthDate);
                        cmd.Parameters.AddWithValue("@ContactInfo",
                            patient.ContactInfo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Diagnosis",
                            patient.Diagnosis ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WardId",
                            patient.WardId ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();

                        if (oldPatient.WardId != patient.WardId)
                        {
                            if (oldPatient.WardId.HasValue)
                                UpdateWardOccupancy(oldPatient.WardId.Value, -1, conn);
                            if (patient.WardId.HasValue)
                                UpdateWardOccupancy(patient.WardId.Value, 1, conn);
                        }

                        if (UserSession.IsAuthenticated)
                        {
                            authService.AddAuditLog(
                                UserSession.CurrentUser.UserId,
                                "UPDATE_PATIENT",
                                "Patient",
                                patient.PatientId,
                                string.Format("Обновлены данные: {0}", patient.FullName));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка при обновлении пациента: {0}", ex.Message), ex);
            }
        }


        // выписка пациента
        public void DischargePatient(int patientId, DateTime dischargeDate,
                                     string reason, string finalDiagnosis)
        {
            // проверка прав (только врач)
            if (!UserSession.CanEditMedicalRecords())
            {
                throw new UnauthorizedAccessException(
                    "Недостаточно прав для выписки пациента. " +
                    "Выписку может оформить только врач.");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // получаем пациента
                    var patient = GetPatientById(patientId);
                    if (patient == null)
                    {
                        throw new Exception("Пациент не найден");
                    }

                    // проверка - пациент уже выписан?
                    if (patient.Status == "discharged")
                    {
                        throw new InvalidOperationException(
                            "Пациент уже выписан! Повторная выписка невозможна.");
                    }

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. обновляем статус пациента
                            string updatePatientQuery = @"
                                UPDATE Patients SET
                                    Status = 'discharged',
                                    DischargeDate = @DischargeDate,
                                    WardId = NULL
                                WHERE PatientId = @PatientId";

                            using (var cmd = new SqlCommand(updatePatientQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate);
                                cmd.Parameters.AddWithValue("@PatientId", patientId);
                                cmd.ExecuteNonQuery();
                            }

                            // 2. освобождаем место в палате
                            if (patient.WardId.HasValue)
                            {
                                string updateWardQuery = @"
                                    UPDATE Wards 
                                    SET OccupiedBeds = OccupiedBeds - 1 
                                    WHERE WardId = @WardId";

                                using (var cmd = new SqlCommand(updateWardQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@WardId", patient.WardId.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // 3. сохраняем информацию о выписке
                            string insertDischargeQuery = @"
                                INSERT INTO Discharges 
                                (PatientId, DischargeDate, Reason, FinalDiagnosis, DischargedBy)
                                VALUES 
                                (@PatientId, @DischargeDate, @Reason, @FinalDiagnosis, @DischargedBy)";

                            using (var cmd = new SqlCommand(insertDischargeQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientId", patientId);
                                cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate);
                                cmd.Parameters.AddWithValue("@Reason", reason);
                                cmd.Parameters.AddWithValue("@FinalDiagnosis", finalDiagnosis);
                                cmd.Parameters.AddWithValue("@DischargedBy",
                                    UserSession.CurrentUser.UserId);
                                cmd.ExecuteNonQuery();
                            }

                            // 4. регистрируем в журнале аудита
                            string auditDetails = string.Format(
                                "Пациент: {0}, Причина: {1}",
                                patient.FullName, reason);

                            string insertAuditQuery = @"
                                INSERT INTO AuditLog 
                                (UserId, Action, EntityType, EntityId, Details)
                                VALUES 
                                (@UserId, @Action, @EntityType, @EntityId, @Details)";

                            using (var cmd = new SqlCommand(insertAuditQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@UserId",
                                    UserSession.CurrentUser.UserId);
                                cmd.Parameters.AddWithValue("@Action", "DISCHARGE_PATIENT");
                                cmd.Parameters.AddWithValue("@EntityType", "Patient");
                                cmd.Parameters.AddWithValue("@EntityId", patientId);
                                cmd.Parameters.AddWithValue("@Details", auditDetails);
                                cmd.ExecuteNonQuery();
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
                throw new Exception(string.Format("Ошибка при выписке пациента: {0}", ex.Message), ex);
            }
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

        private string GenerateCardNumber(SqlConnection conn)
        {
            int year = DateTime.Now.Year;

            string query = @"
                SELECT ISNULL(MAX(CAST(RIGHT(CardNumber, 3) AS INT)), 0)
                FROM Patients
                WHERE CardNumber LIKE @Pattern";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Pattern", string.Format("MK-{0}-%", year));
                int lastNumber = (int)cmd.ExecuteScalar();
                return string.Format("MK-{0}-{1:000}", year, lastNumber + 1);
            }
        }

        private void UpdateWardOccupancy(int wardId, int change, SqlConnection conn)
        {
            string query = @"
                UPDATE Wards 
                SET OccupiedBeds = OccupiedBeds + @Change 
                WHERE WardId = @WardId";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Change", change);
                cmd.Parameters.AddWithValue("@WardId", wardId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}