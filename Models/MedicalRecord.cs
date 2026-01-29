using System;
using System.ComponentModel;

namespace PsychiatricHospitalWPF.Models
{
    public class MedicalRecord : INotifyPropertyChanged
    {
        private int recordId;
        private int patientId;
        private int doctorId;
        private DateTime recordDate;
        private string description;
        private DateTime createdAt;
        private string doctorName;
        private string patientName;
        private string recordType;

        public int RecordId
        {
            get { return recordId; }
            set { recordId = value; OnPropertyChanged(nameof(RecordId)); }
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

        public DateTime RecordDate
        {
            get { return recordDate; }
            set { recordDate = value; OnPropertyChanged(nameof(RecordDate)); }
        }

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged(nameof(Description)); }
        }

        public DateTime CreatedAt
        {
            get { return createdAt; }
            set { createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

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

        // тип записи
        public string RecordType
        {
            get { return recordType; }
            set { recordType = value; OnPropertyChanged(nameof(RecordType)); OnPropertyChanged(nameof(RecordTypeIcon)); OnPropertyChanged(nameof(RecordTypeDisplay)); }
        }

        // иконка для типа записи
        public string RecordTypeIcon
        {
            get
            {
                if (string.IsNullOrEmpty(RecordType))
                    return "📋";

                switch (RecordType.ToLower())
                {
                    case "осмотр":
                    case "examination":
                        return "👨‍⚕️";
                    case "консультация":
                    case "consultation":
                        return "💬";
                    case "изменение_состояния":
                    case "state_change":
                        return "📊";
                    case "анализы":
                    case "tests":
                        return "🔬";
                    default:
                        return "📋";
                }
            }
        }

        // отображаемое название типа
        public string RecordTypeDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(RecordType))
                    return "Запись";

                switch (RecordType.ToLower())
                {
                    case "осмотр":
                    case "examination":
                        return "Осмотр";
                    case "консультация":
                    case "consultation":
                        return "Консультация";
                    case "изменение_состояния":
                    case "state_change":
                        return "Изменение состояния";
                    case "анализы":
                    case "tests":
                        return "Анализы";
                    default:
                        return "Запись";
                }
            }
        }

        // форматированная дата для отображения
        public string FormattedDate
        {
            get { return RecordDate.ToString("dd.MM.yyyy HH:mm"); }
        }

        public string DayOfWeek
        {
            get
            {
                string[] days = { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };
                return days[(int)RecordDate.DayOfWeek];
            }
        }

        // группировка по дате (без времени)
        public string DateGroup
        {
            get { return RecordDate.ToString("dd.MM.yyyy"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return string.Format("{0:dd.MM.yyyy HH:mm} - {1}", RecordDate, DoctorName);
        }
    }
}