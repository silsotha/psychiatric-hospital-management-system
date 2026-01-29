using System.ComponentModel;
using System.Windows.Media;

namespace PsychiatricHospitalWPF.Models
{
    public class Ward : INotifyPropertyChanged
    {
        private int wardId;
        private string wardNumber;
        private string department;
        private int totalBeds;
        private int occupiedBeds;

        public int WardId
        {
            get => wardId;
            set
            {
                wardId = value;
                OnPropertyChanged(nameof(WardId));
            }
        }

        public string WardNumber
        {
            get => wardNumber;
            set
            {
                wardNumber = value;
                OnPropertyChanged(nameof(WardNumber));
            }
        }

        public string Department
        {
            get => department;
            set
            {
                department = value;
                OnPropertyChanged(nameof(Department));
            }
        }

        public int TotalBeds
        {
            get => totalBeds;
            set
            {
                totalBeds = value;
                OnPropertyChanged(nameof(TotalBeds));
                OnPropertyChanged(nameof(AvailableBeds));
                OnPropertyChanged(nameof(OccupancyRate));
                OnPropertyChanged(nameof(OccupancyDisplay));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public int OccupiedBeds
        {
            get => occupiedBeds;
            set
            {
                occupiedBeds = value;
                OnPropertyChanged(nameof(OccupiedBeds));
                OnPropertyChanged(nameof(AvailableBeds));
                OnPropertyChanged(nameof(OccupancyRate));
                OnPropertyChanged(nameof(OccupancyDisplay));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        // вычисляемые свойства
        public int AvailableBeds => TotalBeds - OccupiedBeds;

        public double OccupancyRate
        {
            get
            {
                if (TotalBeds == 0) return 0;
                return (double)OccupiedBeds / TotalBeds * 100;
            }
        }

        public string OccupancyDisplay => string.Format("{0:F1}%", OccupancyRate);

        /// <summary>
        /// цвет статуса в виде строки (для XAML Binding)
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (AvailableBeds == 0)
                    return "#f44336"; // красный = полностью занята
                else if (OccupancyRate >= 80)
                    return "#FF9800"; // оранжевый = почти полная (80-99%)
                else if (OccupancyRate >= 50)
                    return "#FFC107"; // жёлтый = заполнена наполовину (50-79%)
                else
                    return "#4CAF50"; // зелёный = есть места (< 50%)
            }
        }

        /// <summary>
        /// brush для UI
        /// </summary>
        public SolidColorBrush StatusBrush
        {
            get
            {
                var converter = new BrushConverter();
                return (SolidColorBrush)converter.ConvertFromString(StatusColor);
            }
        }

        /// <summary>
        /// текстовое описание статуса
        /// </summary>
        public string StatusText
        {
            get
            {
                if (AvailableBeds == 0)
                    return "Полностью занята";
                else if (OccupancyRate >= 80)
                    return "Почти полная";
                else if (OccupancyRate >= 50)
                    return "Заполнена наполовину";
                else
                    return "Есть места";
            }
        }

        /// <summary>
        /// информация о палате для отображения
        /// </summary>
        public string WardInfo => string.Format("{0} - {1}", WardNumber, Department);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}