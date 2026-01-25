using PsychiatricHospitalWPF.Models;

namespace PsychiatricHospitalWPF.Utils
{
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }
        public static bool IsAuthenticated => CurrentUser != null;

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool CanAccessPatients()
        {
            return IsAuthenticated &&
                   (CurrentUser.IsDoctor || CurrentUser.IsAdmin);
        }

        public static bool CanEditMedicalRecords()
        {
            return IsAuthenticated && CurrentUser.IsDoctor;
        }
    }
}