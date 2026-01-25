using System;
using System.ComponentModel;

namespace PsychiatricHospitalWPF.Models
{
    public class Prescription : INotifyPropertyChanged
    {
        private int prescriptionId;
        private int patientId;
        private int doctorId;
        private string prescriptionType;
        private string name;
        private string dosage;
        private string frequency;
        private int duration;
        private DateTime startDate;
        private DateTime endDate;
        private string status;
        private string cancelReason;
        private string notes;
        private DateTime createdAt;

        // доп. для назначений
        private string doctorName;
        private string patientName;
        private int executionCount;

        public int PrescriptionId
        {
            get { return prescriptionId; }
            set { prescriptionId = value; OnPropertyChanged(nameof(PrescriptionId)); }
        }

        public int PatientId
        {
            get { return patientId; }
            set { patientId = value; OnPropertyChanged(nameof(PatientId)); }
        }

        public int DoctorId
        {
            get { return doctorId; }
            set { doctorId = value; OnPropertyChanged(nameof(DoctorId)); }
        }

        public string PrescriptionType
        {
            get { return prescriptionType; }
            set { prescriptionType = value; OnPropertyChanged(nameof(PrescriptionType)); OnPropertyChanged(nameof(TypeIcon)); }
        }

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Dosage
        {
            get { return dosage; }
            set { dosage = value; OnPropertyChanged(nameof(Dosage)); OnPropertyChanged(nameof(FullName)); }
        }

        public string Frequency
        {
            get { return frequency; }
            set { frequency = value; OnPropertyChanged(nameof(Frequency)); }
        }

        public int Duration
        {
            get { return duration; }
            set { duration = value; OnPropertyChanged(nameof(Duration)); OnPropertyChanged(nameof(DurationDisplay)); }
        }

        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; OnPropertyChanged(nameof(StartDate)); OnPropertyChanged(nameof(PeriodDisplay)); }
        }

        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; OnPropertyChanged(nameof(EndDate)); OnPropertyChanged(nameof(PeriodDisplay)); OnPropertyChanged(nameof(DaysLeft)); }
        }

        public string Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(StatusDisplay)); OnPropertyChanged(nameof(StatusIcon)); }
        }

        public string CancelReason
        {
            get { return cancelReason; }
            set { cancelReason = value; OnPropertyChanged(nameof(CancelReason)); }
        }

        public string Notes
        {
            get { return notes; }
            set { notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public DateTime CreatedAt
        {
            get { return createdAt; }
            set { createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        // доп. для назначений
        public string DoctorName
        {
            get { return doctorName; }
            set { doctorName = value; OnPropertyChanged(nameof(DoctorName)); }
        }

        public string PatientName
        {
            get { return patientName; }
            set { patientName = value; OnPropertyChanged(nameof(PatientName)); }
        }

        public int ExecutionCount
        {
            get { return executionCount; }
            set { executionCount = value; OnPropertyChanged(nameof(ExecutionCount)); }
        }

        // вычисляемые свойства
        public string TypeIcon
        {
            get
            {
                return PrescriptionType == "Медикамент" ? "💊" : "🏥";
            }
        }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(Dosage) && PrescriptionType == "Медикамент")
                    return string.Format("{0} ({1})", Name, Dosage);
                return Name;
            }
        }

        public string StatusDisplay
        {
            get
            {
                switch (Status)
                {
                    case "Активно": return "Активно";
                    case "Завершено": return "Завершено";
                    case "Отменено": return "Отменено";
                    default: return Status;
                }
            }
        }

        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case "Активно": return "🟢";
                    case "Завершено": return "✅";
                    case "Отменено": return "❌";
                    default: return "⚪";
                }
            }
        }

        public string DurationDisplay
        {
            get { return string.Format("{0} дн.", Duration); }
        }

        public string PeriodDisplay
        {
            get
            {
                return string.Format("{0:dd.MM.yyyy} - {1:dd.MM.yyyy}",
                    StartDate, EndDate);
            }
        }

        public int DaysLeft
        {
            get
            {
                if (Status != "Активно")
                    return 0;

                var days = (EndDate - DateTime.Now.Date).Days;
                return days > 0 ? days : 0;
            }
        }

        public string DaysLeftDisplay
        {
            get
            {
                if (Status != "Активно")
                    return "-";

                int days = DaysLeft;
                if (days == 0)
                    return "Последний день";
                else if (days == 1)
                    return "1 день";
                else
                    return string.Format("{0} дн.", days);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return string.Format("{0} {1} - {2}", TypeIcon, FullName, StatusDisplay);
        }
    }
}