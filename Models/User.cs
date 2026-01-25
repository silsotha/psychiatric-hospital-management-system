using System;

namespace PsychiatricHospitalWPF.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }

        public bool IsDoctor
        {
            get { return Role == "doctor"; }
        }

        public bool IsNurse
        {
            get { return Role == "nurse"; }
        }

        public bool IsAdmin
        {
            get { return Role == "admin"; }
        }

        public string RoleDisplayName
        {
            get
            {
                if (Role == "doctor")
                    return "Врач";
                else if (Role == "nurse")
                    return "Медсестра";
                else if (Role == "admin")
                    return "Администратор";
                else
                    return Role;
            }
        }
        public override string ToString()
        {
            return string.Format("{0} ({1})", FullName, RoleDisplayName);
        }
    }
}