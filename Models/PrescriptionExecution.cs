using System;
using System.ComponentModel;

namespace PsychiatricHospitalWPF.Models
{
    public class PrescriptionExecution : INotifyPropertyChanged
    {
        private int executionId;
        private int prescriptionId;
        private int executedBy;
        private DateTime executionDate;
        private string notes;
        private DateTime createdAt;

        private string executedByName;
        private string prescriptionName;

        public int ExecutionId
        {
            get { return executionId; }
            set { executionId = value; OnPropertyChanged(nameof(ExecutionId)); }
        }

        public int PrescriptionId
        {
            get { return prescriptionId; }
            set { prescriptionId = value; OnPropertyChanged(nameof(PrescriptionId)); }
        }

        public int ExecutedBy
        {
            get { return executedBy; }
            set { executedBy = value; OnPropertyChanged(nameof(ExecutedBy)); }
        }

        public DateTime ExecutionDate
        {
            get { return executionDate; }
            set { executionDate = value; OnPropertyChanged(nameof(ExecutionDate)); OnPropertyChanged(nameof(FormattedDate)); }
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

        public string ExecutedByName
        {
            get { return executedByName; }
            set { executedByName = value; OnPropertyChanged(nameof(ExecutedByName)); }
        }

        public string PrescriptionName
        {
            get { return prescriptionName; }
            set { prescriptionName = value; OnPropertyChanged(nameof(PrescriptionName)); }
        }

        public string FormattedDate
        {
            get { return ExecutionDate.ToString("dd.MM.yyyy HH:mm"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", FormattedDate, ExecutedByName);
        }
    }
}