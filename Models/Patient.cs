using System;
using System.ComponentModel;

namespace PsychiatricHospitalWPF.Models
{
    public class Patient : INotifyPropertyChanged
    {
        private int patientId;
        private string cardNumber;
        private string fullName;
        private DateTime birthDate;
        private string diagnosis;
        private string status;
        private string wardNumber;

        public int PatientId
        {
            get { return patientId; }
            set { patientId = value; OnPropertyChanged(nameof(PatientId)); }
        }

        public string CardNumber
        {
            get { return cardNumber; }
            set { cardNumber = value; OnPropertyChanged(nameof(CardNumber)); }
        }

        public string FullName
        {
            get { return fullName; }
            set { fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        public DateTime BirthDate
        {
            get { return birthDate; }
            set
            {
                birthDate = value;
                OnPropertyChanged(nameof(BirthDate));
                OnPropertyChanged(nameof(Age));
                OnPropertyChanged(nameof(BirthDateWithAge));
            }
        }

        public string ContactInfo { get; set; }
        public DateTime AdmissionDate { get; set; }

        public string Diagnosis
        {
            get { return diagnosis; }
            set { diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); }
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }

        public DateTime? DischargeDate { get; set; }
        public int? WardId { get; set; }

        public string WardNumber
        {
            get { return wardNumber; }
            set { wardNumber = value; OnPropertyChanged(nameof(WardNumber)); }
        }

        // подсчёт возраста для отображения
        public int Age
        {
            get
            {
                int age = DateTime.Now.Year - BirthDate.Year;
                if (DateTime.Now.DayOfYear < BirthDate.DayOfYear)
                    age--;
                return age;
            }
        }

        public string StatusDisplay
        {
            get { return Status == "active" ? "На лечении" : "Выписан"; }
        }

        // дата рождения с возрастом
        public string BirthDateWithAge
        {
            get { return string.Format("{0:dd.MM.yyyy} ({1} лет)", BirthDate, Age); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}