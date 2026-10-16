using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace UniversalDownloaderWPF.Models
{
    public class DownloadTask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _id;
        private string _query;
        private string _status = "Queued";
        private double _progress = 0;
        private string _speed = "0 KB/s";
        private string _eta = "--:--";
        private string _filename = "";
        private string _error = "";
        private int _retries = 0;
        private int _trackNumber = 0;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Query
        {
            get => _query;
            set { _query = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public string Speed
        {
            get => _speed;
            set { _speed = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedText)); }
        }

        public string Eta
        {
            get => _eta;
            set { _eta = value; OnPropertyChanged(); OnPropertyChanged(nameof(EtaText)); }
        }

        public string Filename
        {
            get => _filename;
            set { _filename = value; OnPropertyChanged(); }
        }

        public string Error
        {
            get => _error;
            set { _error = value; OnPropertyChanged(); }
        }

        public int Retries
        {
            get => _retries;
            set { _retries = value; OnPropertyChanged(); }
        }

        public int TrackNumber
        {
            get => _trackNumber;
            set { _trackNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        // Computed properties for UI binding
        public string DisplayName
        {
            get
            {
                if (_trackNumber > 0)
                    return $"Track {_trackNumber}";

                if (string.IsNullOrEmpty(_query))
                    return "Unknown";

                return _query.Length > 50 ? _query.Substring(0, 50) + "..." : _query;
            }
        }

        public string StatusText
        {
            get
            {
                switch (_status)
                {
                    case "Queued": return "⏳ Queued";
                    case "Downloading": return "⬇️ Downloading";
                    case "Completed": return "✅ Completed";
                    case "Failed": return "❌ Failed";
                    case "Cancelled": return "🚫 Cancelled";
                    default: return _status;
                }
            }
        }

        public Brush StatusColor
        {
            get
            {
                switch (_status)
                {
                    case "Queued": return new SolidColorBrush(Color.FromRgb(176, 176, 192));
                    case "Downloading": return new SolidColorBrush(Color.FromRgb(32, 128, 255));
                    case "Completed": return new SolidColorBrush(Color.FromRgb(32, 230, 64));
                    case "Failed": return new SolidColorBrush(Color.FromRgb(255, 64, 64));
                    case "Cancelled": return new SolidColorBrush(Color.FromRgb(230, 128, 16));
                    default: return new SolidColorBrush(Colors.White);
                }
            }
        }

        public string SpeedText => $"⚡ {_speed}";
        public string EtaText => $"⏱ {_eta}";
        public string ProgressText => $"{_progress:F0}%";

        public void UpdateFrom(DownloadTask other)
        {
            Status = other.Status;
            Progress = other.Progress;
            Speed = other.Speed;
            Eta = other.Eta;
            Filename = other.Filename;
            Error = other.Error;
            Retries = other.Retries;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
