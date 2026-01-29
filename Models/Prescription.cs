using PsychiatricHospitalWPF.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

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
        private DateTime? canceledAt;
        private int? canceledBy;
        private DateTime? lastExecutionTime;

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
            set { status = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(StatusDisplay)); }
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

        public DateTime? CanceledAt
        {
            get { return canceledAt; }
            set { canceledAt = value; OnPropertyChanged(nameof(CanceledAt)); }
        }

        public int? CanceledBy
        {
            get { return canceledBy; }
            set { canceledBy = value; OnPropertyChanged(nameof(CanceledBy)); }
        }

        /// <summary>
        /// рассчитывает время ближайшего приёма (с учётом выполненных)
        /// </summary>
        public DateTime? NextDueTime
        {
            get
            {
                if (Status != "Активно")
                    return null;

                if (Frequency == "По необходимости")
                    return null;

                var todaySchedule = GetTodaySchedule();
                if (todaySchedule.Count == 0)
                    return null;

                var now = DateTime.Now;
                DateTime? closestTime = null;
                double closestDiff = double.MaxValue;

                // проверяем расписание сегодня
                foreach (var time in todaySchedule)
                {
                    var scheduledTime = now.Date.Add(time);

                    // пропускаем приёмы, которые уже выполнены
                    if (LastExecutionTime.HasValue && scheduledTime <= LastExecutionTime.Value)
                    {
                        continue; // этот приём уже выполнен, переходим к следующему
                    }

                    var diff = Math.Abs((scheduledTime - now).TotalMinutes);

                    // если это время ещё не прошло больше чем на 4 часа
                    if ((scheduledTime - now).TotalHours > -4)
                    {
                        if (diff < closestDiff)
                        {
                            closestDiff = diff;
                            closestTime = scheduledTime;
                        }
                    }
                }

                // если нашли подходящее время сегодня
                if (closestTime.HasValue)
                    return closestTime.Value;

                // проверяем расписание завтра
                var tomorrow = now.Date.AddDays(1);
                foreach (var time in todaySchedule)
                {
                    var scheduledTime = tomorrow.Add(time);

                    // пропускаем выполненные приёмы завтра
                    if (LastExecutionTime.HasValue && scheduledTime <= LastExecutionTime.Value)
                    {
                        continue;
                    }

                    // возвращаем первый невыполненный приём завтра
                    return scheduledTime;
                }

                // если все приёмы завтра тоже выполнены, показываем первый приём послезавтра
                return tomorrow.AddDays(1).Add(todaySchedule[0]);
            }
        }

        /// <summary>
        /// получает расписание времени приёма на день
        /// </summary>
        private List<TimeSpan> GetTodaySchedule()
        {
            if (string.IsNullOrEmpty(Frequency))
                return new List<TimeSpan>();

            switch (Frequency)
            {
                case "Один раз в день":
                    return new List<TimeSpan> { new TimeSpan(9, 0, 0) }; // 09:00

                case "Два раза в день (утро, вечер)":
                    return new List<TimeSpan>
                    {
                        new TimeSpan(8, 0, 0),   // 08:00
                        new TimeSpan(20, 0, 0)   // 20:00
                    };

                case "Три раза в день (утро, день, вечер)":
                    return new List<TimeSpan>
                    {
                        new TimeSpan(8, 0, 0),   // 08:00
                        new TimeSpan(14, 0, 0),  // 14:00
                        new TimeSpan(20, 0, 0)   // 20:00
                    };

                case "Четыре раза в день (каждые 6 часов)":
                    return new List<TimeSpan>
                    {
                        new TimeSpan(6, 0, 0),   // 06:00
                        new TimeSpan(12, 0, 0),  // 12:00
                        new TimeSpan(18, 0, 0),  // 18:00
                        new TimeSpan(0, 0, 0)    // 00:00 (полночь)
                    };

                default:
                    return new List<TimeSpan>();
            }
        }

        /// <summary>
        /// время последнего выполнения назначения
        /// </summary>
        public DateTime? LastExecutionTime
        {
            get => lastExecutionTime;
            set
            {
                lastExecutionTime = value;
                OnPropertyChanged(nameof(LastExecutionTime));
                // бновить зависимые свойства
                OnPropertyChanged(nameof(NextDueTime));
                OnPropertyChanged(nameof(ExecutionStatusText));
                OnPropertyChanged(nameof(ExecutionStatusBrush));
            }
        }

        /// <summary>
        /// текстовое описание статуса выполнения
        /// </summary>
        public string ExecutionStatusText
        {
            get
            {
                if (Status != "Активно")
                    return "-";

                var nextDue = NextDueTime;
                if (!nextDue.HasValue)
                    return "По необходимости";

                var timeDiff = (nextDue.Value - DateTime.Now).TotalMinutes;

                // просрочено более 30 минут
                if (timeDiff < -30)
                {
                    int minutesLate = (int)Math.Abs(timeDiff);
                    if (minutesLate >= 60)
                    {
                        int hoursLate = minutesLate / 60;
                        return string.Format("Просрочено на {0} ч", hoursLate);
                    }
                    return string.Format("Просрочено на {0} мин", minutesLate);
                }

                // просрочено менее 30 минут
                if (timeDiff < 0)
                    return "Время приёма!";

                // до приёма менее 30 минут
                if (timeDiff <= 30)
                    return string.Format("Через {0} мин", (int)timeDiff);

                // запланировано на будущее
                if (nextDue.Value.Date == DateTime.Now.Date)
                {
                    // сегодня
                    return string.Format("Сегодня в {0:HH:mm}", nextDue.Value);
                }
                else if (nextDue.Value.Date == DateTime.Now.Date.AddDays(1))
                {
                    // завтра
                    return string.Format("Завтра в {0:HH:mm}", nextDue.Value);
                }
                else
                {
                    // другой день
                    return nextDue.Value.ToString("dd.MM HH:mm");
                }
            }
        }

        /// <summary>
        /// цветной индикатор статуса выполнения
        /// </summary>
        public SolidColorBrush ExecutionStatusBrush
        {
            get
            {
                if (Status != "Активно")
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // серый (#9E9E9E)

                var nextDue = NextDueTime;
                if (!nextDue.HasValue)
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // серый

                var timeDiff = (nextDue.Value - DateTime.Now).TotalMinutes;

                // красный: просрочено более 30 минут
                if (timeDiff < -30)
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // #f44336

                // оранжевый: просрочено 0-30 минут
                if (timeDiff < 0)
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // #FF9800

                // жёлтый: до приёма менее 30 минут
                if (timeDiff <= 30)
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // #FFC107

                // зелёный: запланировано
                return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50
            }
        }

        /// <summary>
        /// кисть для цветного индикатора статуса назначения
        /// </summary>
        public SolidColorBrush StatusBrush
        {
            get
            {
                switch (Status)
                {
                    case "Активно":
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // зелёный #4CAF50

                    case "Завершено":
                        return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // синий #2196F3

                    case "Отменено":
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // красный #f44336

                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // серый #9E9E9E
                }
            }
        }

        // информация об отмене для отображения
        public string CancelInfo
        {
            get
            {
                if (Status != "Отменено") return string.Empty;

                string info = "❌ Отменено";

                if (CanceledAt.HasValue)
                    info += string.Format(": {0:dd.MM.yyyy HH:mm}", CanceledAt.Value);

                if (!string.IsNullOrEmpty(CancelReason))
                    info += string.Format("\nПричина: {0}", CancelReason);

                return info;
            }
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