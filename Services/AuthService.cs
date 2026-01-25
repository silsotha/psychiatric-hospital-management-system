using PsychiatricHospitalWPF.Models;
using PsychiatricHospitalWPF.Utils;
using System;
using System.Data.SqlClient;

namespace PsychiatricHospitalWPF.Services
{
    public class AuthService
    {
        public User Authenticate(string username, string password)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT UserId, Username, PasswordHash, FullName, 
                           Role, IsActive, LastLogin
                    FROM Users
                    WHERE Username = @Username AND IsActive = 1";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["PasswordHash"].ToString();

                            if (PasswordHelper.VerifyPassword(password, storedHash))
                            {
                                var user = new User
                                {
                                    UserId = (int)reader["UserId"],
                                    Username = reader["Username"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Role = reader["Role"].ToString(),
                                    IsActive = (bool)reader["IsActive"],
                                    LastLogin = reader["LastLogin"] != DBNull.Value
                                        ? (DateTime?)reader["LastLogin"]
                                        : null
                                };

                                reader.Close();
                                UpdateLastLogin(user.UserId, conn);
                                return user;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private void UpdateLastLogin(int userId, SqlConnection conn)
        {
            string query = "UPDATE Users SET LastLogin = GETDATE() WHERE UserId = @UserId";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public void AddAuditLog(int userId, string action, string entityType,
                                int? entityId = null, string details = null)
        {
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO AuditLog (UserId, Action, EntityType, EntityId, Details)
                        VALUES (@UserId, @Action, @EntityType, @EntityId, @Details)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@EntityType", entityType);
                        cmd.Parameters.AddWithValue("@EntityId",
                            entityId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Details",
                            details ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }
    }
}