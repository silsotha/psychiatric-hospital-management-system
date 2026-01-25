using System.Data.SqlClient;

namespace PsychiatricHospitalWPF.Utils
{
    public static class DatabaseConnection
    {
        private static string connectionString =
            @"Data Source=localhost\SQLExpress;
              Initial Catalog=PsychiatricHospitalDB;
              Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch { return false; }
        }
    }
}