using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Utils;

namespace PsychiatricHospitalWPF.Services
{
    public class PrescriptionService
    {
        private readonly AuthService authService = new AuthService();

        // создать новое назначение
        public int CreatePrescription(Prescription prescription)
        {
            // проверка прав (только врач)
            if (!UserSession.CanEditMedicalRecords())
            {
                throw new UnauthorizedAccessException(
                    "Только врачи могут создавать назначения");
            }

            // валидация обязательных полей
            if (string.IsNullOrWhiteSpace(prescription.PrescriptionType))
                throw new ArgumentException("Укажите тип назначения");

            if (string.IsNullOrWhiteSpace(prescription.Name))
                throw new ArgumentException("Укажите название препарата/процедуры");

            if (prescription.PrescriptionType == "Медикамент" &&
                string.IsNullOrWhiteSpace(prescription.Dosage))
                throw new ArgumentException("Укажите дозировку для медикамента");

            if (string.IsNullOrWhiteSpace(prescription.Frequency))
                throw new ArgumentException("Укажите периодичность приема");

            if (prescription.Duration <= 0)
                throw new ArgumentException("Длительность курса должна быть больше 0");

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // расчет даты окончания
                    DateTime endDate = prescription.StartDate.AddDays(prescription.Duration);

                    string query = @"
                        INSERT INTO Prescriptions 
                        (PatientId, DoctorId, PrescriptionType, Name, Dosage, 
                         Frequency, Duration, StartDate, EndDate, Status, Notes)
                        OUTPUT INSERTED.PrescriptionId
                        VALUES 
                        (@PatientId, @DoctorId, @PrescriptionType, @Name, @Dosage, 
                         @Frequency, @Duration, @StartDate, @EndDate, @Status, @Notes)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", prescription.PatientId);
                        cmd.Parameters.AddWithValue("@DoctorId", UserSession.CurrentUser.UserId);
                        cmd.Parameters.AddWithValue("@PrescriptionType", prescription.PrescriptionType);
                        cmd.Parameters.AddWithValue("@Name", prescription.Name);
                        cmd.Parameters.AddWithValue("@Dosage",
                            prescription.Dosage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Frequency", prescription.Frequency);
                        cmd.Parameters.AddWithValue("@Duration", prescription.Duration);
                        cmd.Parameters.AddWithValue("@StartDate", prescription.StartDate);
                        cmd.Parameters.AddWithValue("@EndDate", endDate);
                        cmd.Parameters.AddWithValue("@Status", "Активно");
                        cmd.Parameters.AddWithValue("@Notes",
                            prescription.Notes ?? (object)DBNull.Value);

                        int prescriptionId = (int)cmd.ExecuteScalar();

                        // регистрация в аудите
                        authService.AddAuditLog(
                            UserSession.CurrentUser.UserId,
                            "CREATE_PRESCRIPTION",
                            "Prescription",
                            prescriptionId,
                            string.Format("Создано назначение: {0} для пациента ID:{1}",
                                prescription.Name, prescription.PatientId));

                        return prescriptionId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка создания назначения: {0}", ex.Message), ex);
            }
        }

        public List<Prescription> GetPrescriptionsByPatient(int patientId, bool activeOnly = false)
        {
            var prescriptions = new List<Prescription>();

            try
            {
                using (SqlConnection conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    p.PrescriptionId,
                    p.PatientId,
                    p.DoctorId,
                    p.PrescriptionType,
                    p.Name,
                    p.Dosage,
                    p.Frequency,
                    p.Duration,
                    p.StartDate,
                    p.EndDate,
                    p.Status,
                    p.CancelReason,
                    p.Notes,
                    p.CreatedAt,
                    p.CanceledAt,
                    p.CanceledBy,
                    u.FullName as DoctorName,
                    -- КЛЮЧЕВОЙ ПОДЗАПРОС: Получаем время последнего выполнения
                    (SELECT TOP 1 ExecutionDate 
                     FROM PrescriptionExecutions 
                     WHERE PrescriptionId = p.PrescriptionId 
                     ORDER BY ExecutionDate DESC) as LastExecutionTime
                FROM Prescriptions p
                LEFT JOIN Users u ON p.DoctorId = u.UserId
                WHERE p.PatientId = @PatientId";

                    if (activeOnly)
                    {
                        query += " AND p.Status = 'Активно'";
                    }

                    query += " ORDER BY p.StartDate DESC, p.CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var prescription = new Prescription
                                {
                                    PrescriptionId = reader.GetInt32(0),
                                    PatientId = reader.GetInt32(1),
                                    DoctorId = reader.GetInt32(2),
                                    PrescriptionType = reader.GetString(3),
                                    Name = reader.GetString(4),
                                    Dosage = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Frequency = reader.GetString(6),
                                    Duration = reader.GetInt32(7),
                                    StartDate = reader.GetDateTime(8),
                                    EndDate = reader.GetDateTime(9),
                                    Status = reader.GetString(10),
                                    CancelReason = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    Notes = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    CreatedAt = reader.GetDateTime(13),
                                    CanceledAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14),
                                    CanceledBy = reader.IsDBNull(15) ? (int?)null : reader.GetInt32(15),
                                    DoctorName = reader.IsDBNull(16) ? "Неизвестно" : reader.GetString(16),

                                    // кЛЮЧЕВОЕ: Загружаем время последнего выполнения
                                    LastExecutionTime = reader.IsDBNull(17) ? (DateTime?)null : reader.GetDateTime(17)
                                };

                                prescriptions.Add(prescription);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка загрузки назначений: {0}", ex.Message), ex);
            }

            return prescriptions;
        }

        /// <summary>
        /// получить список назначений, требующих продления
        /// (активные назначения, у которых EndDate через 3 дня или уже прошла)
        /// </summary>
        public List<Prescription> GetExpiringSoonPrescriptions(int doctorId)
        {
            var prescriptions = new List<Prescription>();

            try
            {
                using (SqlConnection conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    p.PrescriptionId,
                    p.PatientId,
                    p.DoctorId,
                    p.PrescriptionType,
                    p.Name,
                    p.Dosage,
                    p.Frequency,
                    p.Duration,
                    p.StartDate,
                    p.EndDate,
                    p.Status,
                    p.CancelReason,
                    p.Notes,
                    p.CreatedAt,
                    p.CanceledAt,
                    p.CanceledBy,
                    u.FullName as DoctorName,
                    pat.FullName as PatientName,
                    (SELECT TOP 1 ExecutionDate 
                     FROM PrescriptionExecutions 
                     WHERE PrescriptionId = p.PrescriptionId 
                     ORDER BY ExecutionDate DESC) as LastExecutionTime
                FROM Prescriptions p
                LEFT JOIN Users u ON p.DoctorId = u.UserId
                LEFT JOIN Patients pat ON p.PatientId = pat.PatientId
                WHERE p.DoctorId = @DoctorId 
                  AND p.Status = N'Активно'
                  AND p.EndDate <= DATEADD(DAY, 3, GETDATE())
                ORDER BY p.EndDate ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var prescription = new Prescription
                                {
                                    PrescriptionId = reader.GetInt32(0),
                                    PatientId = reader.GetInt32(1),
                                    DoctorId = reader.GetInt32(2),
                                    PrescriptionType = reader.GetString(3),
                                    Name = reader.GetString(4),
                                    Dosage = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Frequency = reader.GetString(6),
                                    Duration = reader.GetInt32(7),
                                    StartDate = reader.GetDateTime(8),
                                    EndDate = reader.GetDateTime(9),
                                    Status = reader.GetString(10),
                                    CancelReason = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    Notes = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    CreatedAt = reader.GetDateTime(13),
                                    CanceledAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14),
                                    CanceledBy = reader.IsDBNull(15) ? (int?)null : reader.GetInt32(15),
                                    DoctorName = reader.IsDBNull(16) ? "Неизвестно" : reader.GetString(16),
                                    PatientName = reader.IsDBNull(17) ? "Неизвестно" : reader.GetString(17),
                                    LastExecutionTime = reader.IsDBNull(18) ? (DateTime?)null : reader.GetDateTime(18)
                                };

                                prescriptions.Add(prescription);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения истекающих назначений: {ex.Message}", ex);
            }

            return prescriptions;
        }

        /// <summary>
        /// продлить назначение на указанное количество дней
        /// </summary>
        public void ExtendPrescription(int prescriptionId, int additionalDays, string notes)
        {
            if (!UserSession.CanEditMedicalRecords())
            {
                throw new UnauthorizedAccessException("Только врачи могут продлевать назначения");
            }

            if (additionalDays <= 0)
            {
                throw new ArgumentException("Количество дней должно быть больше 0");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                UPDATE Prescriptions 
                SET EndDate = DATEADD(DAY, @AdditionalDays, EndDate),
                    Duration = Duration + @AdditionalDays,
                    Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 
                            'Продлено на ' + CAST(@AdditionalDays AS NVARCHAR) + ' дн. ' + 
                            FORMAT(GETDATE(), 'dd.MM.yyyy HH:mm') + ': ' + @Notes
                WHERE PrescriptionId = @PrescriptionId 
                  AND Status = N'Активно'";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@AdditionalDays", additionalDays);
                        cmd.Parameters.AddWithValue("@Notes", notes ?? "продление курса");

                        int affected = cmd.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            throw new InvalidOperationException("Назначение не найдено или не активно");
                        }
                    }

                    authService.AddAuditLog(
                        UserSession.CurrentUser.UserId,
                        "EXTEND_PRESCRIPTION",
                        "Prescription",
                        prescriptionId,
                        $"Продление назначения на {additionalDays} дней. {notes}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка продления назначения: {ex.Message}", ex);
            }
        }

        // получить назначение по ID
        public Prescription GetPrescriptionById(int prescriptionId)
        {
            Prescription prescription = null;

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            p.*,
                            u.FullName as DoctorName,
                            pat.FullName as PatientName,
                            (SELECT COUNT(*) FROM PrescriptionExecutions 
                             WHERE PrescriptionId = p.PrescriptionId) as ExecutionCount,
                            (SELECT TOP 1 ExecutionDate 
                             FROM PrescriptionExecutions 
                             WHERE PrescriptionId = p.PrescriptionId 
                             ORDER BY ExecutionDate DESC) as LastExecutionTime
                        FROM Prescriptions p
                        LEFT JOIN Users u ON p.DoctorId = u.UserId
                        LEFT JOIN Patients pat ON p.PatientId = pat.PatientId
                        WHERE p.PrescriptionId = @PrescriptionId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                prescription = new Prescription
                                {
                                    PrescriptionId = (int)reader["PrescriptionId"],
                                    PatientId = (int)reader["PatientId"],
                                    DoctorId = (int)reader["DoctorId"],
                                    PrescriptionType = reader["PrescriptionType"].ToString(),
                                    Name = reader["Name"].ToString(),
                                    Dosage = reader["Dosage"] != DBNull.Value ? reader["Dosage"].ToString() : null,
                                    Frequency = reader["Frequency"].ToString(),
                                    Duration = (int)reader["Duration"],
                                    StartDate = (DateTime)reader["StartDate"],
                                    EndDate = (DateTime)reader["EndDate"],
                                    Status = reader["Status"].ToString(),
                                    CancelReason = reader["CancelReason"] != DBNull.Value ? reader["CancelReason"].ToString() : null,
                                    Notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : null,
                                    CreatedAt = (DateTime)reader["CreatedAt"],
                                    CanceledAt = reader["CanceledAt"] != DBNull.Value ? (DateTime?)reader["CanceledAt"] : null,
                                    CanceledBy = reader["CanceledBy"] != DBNull.Value ? (int?)reader["CanceledBy"] : null,
                                    DoctorName = reader["DoctorName"].ToString(),
                                    PatientName = reader["PatientName"].ToString(),
                                    ExecutionCount = (int)reader["ExecutionCount"],
                                    LastExecutionTime = reader["LastExecutionTime"] != DBNull.Value ? (DateTime?)reader["LastExecutionTime"] : null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения назначения: {0}", ex.Message), ex);
            }

            return prescription;
        }

        // отметить выполнение назначения
        public int ExecutePrescription(int prescriptionId, DateTime executionDate, string notes = null)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // проверка, что назначение активно
                    string checkQuery = "SELECT Status FROM Prescriptions WHERE PrescriptionId = @PrescriptionId";
                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        string status = checkCmd.ExecuteScalar()?.ToString();

                        if (status != "Активно")
                        {
                            throw new InvalidOperationException("Назначение не активно");
                        }
                    }

                    // добавление отметки выполнения
                    string query = @"
                        INSERT INTO PrescriptionExecutions 
                        (PrescriptionId, ExecutedBy, ExecutionDate, Notes)
                        OUTPUT INSERTED.ExecutionId
                        VALUES 
                        (@PrescriptionId, @ExecutedBy, @ExecutionDate, @Notes)";

                    int executionId;
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@ExecutedBy", UserSession.CurrentUser.UserId);
                        cmd.Parameters.AddWithValue("@ExecutionDate", executionDate);
                        cmd.Parameters.AddWithValue("@Notes", notes ?? (object)DBNull.Value);

                        executionId = (int)cmd.ExecuteScalar();
                    }

                    // регистрация в аудите
                    authService.AddAuditLog(
                        UserSession.CurrentUser.UserId,
                        "EXECUTE_PRESCRIPTION",
                        "PrescriptionExecution",
                        executionId,
                        string.Format("Выполнено назначение ID:{0} в {1:dd.MM.yyyy HH:mm}",
                            prescriptionId, executionDate));

                    return executionId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка выполнения назначения: {0}", ex.Message), ex);
            }
        }

        // отменить назначение
        public void CancelPrescription(int prescriptionId, string cancelReason)
        {
            // проверка прав (только врач может отменять)
            if (!UserSession.CanEditMedicalRecords())
            {
                throw new UnauthorizedAccessException("Только врачи могут отменять назначения");
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                throw new ArgumentException("Укажите причину отмены");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // проверка, что назначение можно отменить
                    string checkQuery = @"
                        SELECT Status, 
                               (SELECT COUNT(*) FROM PrescriptionExecutions 
                                WHERE PrescriptionId = @PrescriptionId) as ExecutionCount
                        FROM Prescriptions 
                        WHERE PrescriptionId = @PrescriptionId";

                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string status = reader["Status"].ToString();
                                int executionCount = (int)reader["ExecutionCount"];

                                if (status == "Отменено")
                                {
                                    throw new InvalidOperationException("Назначение уже отменено");
                                }

                                if (status == "Завершено")
                                {
                                    throw new InvalidOperationException("Завершенное назначение нельзя отменить");
                                }

                                if (executionCount > 0)
                                {
                                    throw new InvalidOperationException(
                                        "Нельзя отменить назначение с отметками выполнения. Используйте досрочное завершение.");
                                }
                            }
                            else
                            {
                                throw new Exception("Назначение не найдено");
                            }
                        }
                    }

                    // отмена назначения
                    string updateQuery = @"
                    UPDATE Prescriptions 
                    SET Status = N'Отменено', 
                        CancelReason = @CancelReason,
                        CanceledAt = GETDATE(),
                        CanceledBy = @CanceledBy
                    WHERE PrescriptionId = @PrescriptionId";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@CancelReason", cancelReason);
                        cmd.Parameters.AddWithValue("@CanceledBy", UserSession.CurrentUser.UserId);
                        cmd.ExecuteNonQuery();
                    }

                    // регистрация в аудите
                    authService.AddAuditLog(
                        UserSession.CurrentUser.UserId,
                        "CANCEL_PRESCRIPTION",
                        "Prescription",
                        prescriptionId,
                        string.Format("Отмена назначения. Причина: {0}", cancelReason));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка отмены назначения: {0}", ex.Message), ex);
            }
        }

        // завершить назначение досрочно
        public void CompletePrescription(int prescriptionId, string reason)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        UPDATE Prescriptions 
                        SET Status = N'Завершено', 
                            EndDate = GETDATE(),
                            Notes = ISNULL(Notes, '') + ' Завершено досрочно: ' + @Reason
                        WHERE PrescriptionId = @PrescriptionId AND Status = N'Активно'";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@Reason", reason);

                        int affected = cmd.ExecuteNonQuery();
                        if (affected == 0)
                        {
                            throw new InvalidOperationException("Назначение не активно");
                        }
                    }

                    authService.AddAuditLog(
                        UserSession.CurrentUser.UserId,
                        "COMPLETE_PRESCRIPTION",
                        "Prescription",
                        prescriptionId,
                        string.Format("Досрочное завершение. Причина: {0}", reason));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка завершения назначения: {0}", ex.Message), ex);
            }
        }

        // получить отметки выполнения для назначения
        public List<PrescriptionExecution> GetExecutions(int prescriptionId)
        {
            var executions = new List<PrescriptionExecution>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT pe.*, u.FullName as ExecutedByName, p.Name as PrescriptionName
                        FROM PrescriptionExecutions pe
                        INNER JOIN Users u ON pe.ExecutedBy = u.UserId
                        INNER JOIN Prescriptions p ON pe.PrescriptionId = p.PrescriptionId
                        WHERE pe.PrescriptionId = @PrescriptionId
                        ORDER BY pe.ExecutionDate DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                executions.Add(MapExecution(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения отметок: {0}", ex.Message), ex);
            }

            return executions;
        }

        // маппинг из SqlDataReader в Prescription
        private Prescription MapPrescription(SqlDataReader reader)
        {
            return new Prescription
            {
                PrescriptionId = (int)reader["PrescriptionId"],
                PatientId = (int)reader["PatientId"],
                DoctorId = (int)reader["DoctorId"],
                PrescriptionType = reader["PrescriptionType"].ToString(),
                Name = reader["Name"].ToString(),
                Dosage = reader["Dosage"] != DBNull.Value ? reader["Dosage"].ToString() : null,
                Frequency = reader["Frequency"].ToString(),
                Duration = (int)reader["Duration"],
                StartDate = (DateTime)reader["StartDate"],
                EndDate = (DateTime)reader["EndDate"],
                Status = reader["Status"].ToString(),
                CancelReason = reader["CancelReason"] != DBNull.Value ? reader["CancelReason"].ToString() : null,
                Notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : null,
                CreatedAt = (DateTime)reader["CreatedAt"],
                CanceledAt = reader["CanceledAt"] != DBNull.Value
                    ? (DateTime?)reader["CanceledAt"]
                    : null,
                CanceledBy = reader["CanceledBy"] != DBNull.Value
                    ? (int?)reader["CanceledBy"]
                    : null,
                DoctorName = reader["DoctorName"].ToString(),
                PatientName = reader["PatientName"].ToString(),
                ExecutionCount = (int)reader["ExecutionCount"],
                LastExecutionTime = reader["LastExecutionTime"] != DBNull.Value
                    ? (DateTime?)reader["LastExecutionTime"]
                    : null
            };
        }

        // маппинг из SqlDataReader в PrescriptionExecution
        private PrescriptionExecution MapExecution(SqlDataReader reader)
        {
            return new PrescriptionExecution
            {
                ExecutionId = (int)reader["ExecutionId"],
                PrescriptionId = (int)reader["PrescriptionId"],
                ExecutedBy = (int)reader["ExecutedBy"],
                ExecutionDate = (DateTime)reader["ExecutionDate"],
                Notes = reader["Notes"] != DBNull.Value ? reader["Notes"].ToString() : null,
                CreatedAt = (DateTime)reader["CreatedAt"],
                ExecutedByName = reader["ExecutedByName"].ToString(),
                PrescriptionName = reader["PrescriptionName"].ToString()
            };
        }
    }
}