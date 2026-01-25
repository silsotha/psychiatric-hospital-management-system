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

                        // регистрация в журнале аудита
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

        // получить все назначения пациента
        public List<Prescription> GetPrescriptionsByPatient(int patientId, bool activeOnly = false)
        {
            var prescriptions = new List<Prescription>();

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT p.*, 
                               u.FullName as DoctorName,
                               pat.FullName as PatientName,
                               (SELECT COUNT(*) FROM PrescriptionExecutions WHERE PrescriptionId = p.PrescriptionId) as ExecutionCount
                        FROM Prescriptions p
                        INNER JOIN Users u ON p.DoctorId = u.UserId
                        INNER JOIN Patients pat ON p.PatientId = pat.PatientId
                        WHERE p.PatientId = @PatientId";

                    if (activeOnly)
                    {
                        query += " AND p.Status = N'Активно'";
                    }

                    query += " ORDER BY p.StartDate DESC, p.CreatedAt DESC";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PatientId", patientId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                prescriptions.Add(MapPrescription(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка получения назначений: {0}", ex.Message), ex);
            }

            return prescriptions;
        }

        /// получить назначение по ID
        public Prescription GetPrescriptionById(int prescriptionId)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT p.*, 
                               u.FullName as DoctorName,
                               pat.FullName as PatientName,
                               (SELECT COUNT(*) FROM PrescriptionExecutions WHERE PrescriptionId = p.PrescriptionId) as ExecutionCount
                        FROM Prescriptions p
                        INNER JOIN Users u ON p.DoctorId = u.UserId
                        INNER JOIN Patients pat ON p.PatientId = pat.PatientId
                        WHERE p.PrescriptionId = @PrescriptionId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapPrescription(reader);
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

            return null;
        }

        // отметить выполнение назначения
        public int ExecutePrescription(int prescriptionId, string notes = null)
        {
            // проверка прав (медсестра или врач)
            if (UserSession.CurrentUser.Role != "nurse" &&
                UserSession.CurrentUser.Role != "doctor")
            {
                throw new UnauthorizedAccessException(
                    "Только медсестры и врачи могут отмечать выполнение");
            }

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // проверяем, что назначение активно
                    string checkQuery = "SELECT Status FROM Prescriptions WHERE PrescriptionId = @PrescriptionId";
                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        var status = checkCmd.ExecuteScalar()?.ToString();

                        if (status != "Активно")
                        {
                            throw new InvalidOperationException(
                                string.Format("Назначение не активно (статус: {0})", status));
                        }
                    }

                    // создаем отметку о выполнении
                    string query = @"
                        INSERT INTO PrescriptionExecutions 
                        (PrescriptionId, ExecutedBy, ExecutionDate, Notes)
                        OUTPUT INSERTED.ExecutionId
                        VALUES 
                        (@PrescriptionId, @ExecutedBy, @ExecutionDate, @Notes)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@ExecutedBy", UserSession.CurrentUser.UserId);
                        cmd.Parameters.AddWithValue("@ExecutionDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Notes", notes ?? (object)DBNull.Value);

                        int executionId = (int)cmd.ExecuteScalar();

                        // регистрация в журнале аудита
                        authService.AddAuditLog(
                            UserSession.CurrentUser.UserId,
                            "EXECUTE_PRESCRIPTION",
                            "PrescriptionExecution",
                            executionId,
                            string.Format("Отметка выполнения назначения ID:{0}", prescriptionId));

                        return executionId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Ошибка отметки выполнения: {0}", ex.Message), ex);
            }
        }

        // отменить назначение
        public void CancelPrescription(int prescriptionId, string cancelReason)
        {
            if (string.IsNullOrWhiteSpace(cancelReason))
                throw new ArgumentException("Укажите причину отмены");

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // проверяем права и статус
                    string checkQuery = @"
                        SELECT DoctorId, Status,
                               (SELECT COUNT(*) FROM PrescriptionExecutions WHERE PrescriptionId = @PrescriptionId) as ExecCount
                        FROM Prescriptions 
                        WHERE PrescriptionId = @PrescriptionId";

                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int doctorId = (int)reader["DoctorId"];
                                string status = reader["Status"].ToString();
                                int execCount = (int)reader["ExecCount"];

                                // проверка прав (только создавший врач)
                                if (doctorId != UserSession.CurrentUser.UserId)
                                {
                                    throw new UnauthorizedAccessException(
                                        "Только врач, создавший назначение, может его отменить");
                                }

                                // проверка статуса
                                if (status != "Активно")
                                {
                                    throw new InvalidOperationException(
                                        string.Format("Назначение уже имеет статус: {0}", status));
                                }

                                // проверка наличия выполнений
                                if (execCount > 0)
                                {
                                    throw new InvalidOperationException(
                                        "Нельзя отменить назначение с отметками о выполнении. Завершите его досрочно.");
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
                        SET Status = N'Отменено', CancelReason = @CancelReason
                        WHERE PrescriptionId = @PrescriptionId";

                    using (var cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@PrescriptionId", prescriptionId);
                        cmd.Parameters.AddWithValue("@CancelReason", cancelReason);
                        cmd.ExecuteNonQuery();
                    }

                    // регистрация в журнале аудита
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
                DoctorName = reader["DoctorName"].ToString(),
                PatientName = reader["PatientName"].ToString(),
                ExecutionCount = (int)reader["ExecutionCount"]
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