using AutoUpdaterDotNET;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using UniversalDownloaderWPF.Models;
using UniversalDownloaderWPF.Services;

namespace UniversalDownloaderWPF
{
    public partial class MainWindow : Window
    {
        private readonly DownloadManager _downloadManager;
        private readonly AppConfig _config;
        private readonly DispatcherTimer _updateTimer;
        private readonly ObservableCollection<DownloadTask> _tasks;

        public MainWindow()
        {
            InitializeComponent();
            AutoUpdater.Start("https://raw.githubusercontent.com/BalazsHajdu0/UniversalDownloaderPro/main/update.xml");

            // Load config
            _config = AppConfig.Load();

            // Initialize task collection
            _tasks = new ObservableCollection<DownloadTask>();
            TaskList.ItemsSource = _tasks;

            // Initialize download manager
            _downloadManager = new DownloadManager(_config);
            _downloadManager.TaskUpdated += OnTaskUpdated;
            _downloadManager.Start();

            // Load settings to UI
            LoadSettings();

            // Update timer for stats
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();

            // Check dependencies
            CheckDependencies();
        }

        private void CheckDependencies()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string ytdlpPath = Path.Combine(basePath, "Tools", "yt-dlp.exe");
            string ffmpegPath = Path.Combine(basePath, "Tools", "ffmpeg.exe");

            bool ytdlpOk = File.Exists(ytdlpPath);
            bool ffmpegOk = File.Exists(ffmpegPath);

            if (ytdlpOk && ffmpegOk)
            {
                StatusText.Text = "✅ All systems ready";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(32, 224, 64));
            }
            else
            {
                string missing = "";
                if (!ytdlpOk) missing += "yt-dlp.exe ";
                if (!ffmpegOk) missing += "ffmpeg.exe";
                StatusText.Text = $"⚠️ Missing: {missing.Trim()}";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 64, 64));

                // Auto-download missing tools
                if (MessageBox.Show(
                    "Required tools are missing. Do you want to download them automatically?\n\n" +
                    "This will download:\n" +
                    "• yt-dlp.exe (~10 MB)\n" +
                    "• ffmpeg.exe (~80 MB)\n\n" +
                    "Click Yes to download, or No to download manually later.",
                    "Missing Dependencies",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DownloadTools();
                }
            }
        }

        private async void DownloadTools()
        {
            StatusText.Text = "⏳ Downloading tools...";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 64));

            try
            {
                string toolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools");
                Directory.CreateDirectory(toolsPath);

                using (var client = new System.Net.WebClient())
                {
                    // Download yt-dlp
                    string ytdlpPath = Path.Combine(toolsPath, "yt-dlp.exe");
                    if (!File.Exists(ytdlpPath))
                    {
                        StatusText.Text = "⏳ Downloading yt-dlp.exe... (1/2)";
                        string ytdlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                        await client.DownloadFileTaskAsync(new Uri(ytdlpUrl), ytdlpPath);
                    }

                    // Download ffmpeg
                    string ffmpegPath = Path.Combine(toolsPath, "ffmpeg.exe");
                    if (!File.Exists(ffmpegPath))
                    {
                        StatusText.Text = "⏳ Downloading ffmpeg... (2/2) - This may take a minute...";
                        
                        // Download ffmpeg ZIP from GitHub releases (smaller, faster)
                        string ffmpegZipUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
                        string zipPath = Path.Combine(toolsPath, "ffmpeg.zip");
                        
                        await client.DownloadFileTaskAsync(new Uri(ffmpegZipUrl), zipPath);
                        
                        StatusText.Text = "⏳ Extracting ffmpeg...";
                        
                        // Extract only ffmpeg.exe from the ZIP
                        await Task.Run(() => ExtractFfmpegFromZip(zipPath, toolsPath));
                        
                        // Clean up ZIP
                        if (File.Exists(zipPath))
                            File.Delete(zipPath);
                    }
                }

                CheckDependencies();
                MessageBox.Show("✅ All tools downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Download failed";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 64, 64));
                MessageBox.Show($"Failed to download tools: {ex.Message}\n\nYou can manually download:\n• yt-dlp.exe from github.com/yt-dlp/yt-dlp/releases\n• ffmpeg.exe from gyan.dev/ffmpeg/builds", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractFfmpegFromZip(string zipPath, string toolsPath)
        {
            // Use System.IO.Compression to extract
            using (var archive = System.IO.Compression.ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Find ffmpeg.exe in the archive (it's in a subfolder)
                    if (entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string destPath = Path.Combine(toolsPath, "ffmpeg.exe");
                        entry.ExtractToFile(destPath, true);
                        break;
                    }
                }
            }
        }

        private void LoadSettings()
        {
            FolderTextBox.Text = _config.DownloadFolder;

            SelectComboBoxItem(FormatComboBox, _config.Format);
            SelectComboBoxItem(QualityComboBox, _config.Bitrate);
            SelectComboBoxItem(ParallelComboBox, _config.ParallelDownloads.ToString());
            SelectComboBoxItem(RetryComboBox, _config.MaxRetries.ToString());

            NumberingCheckBox.IsChecked = _config.Numbering;
            MetadataCheckBox.IsChecked = _config.EnableMetadata;
            ThumbnailCheckBox.IsChecked = _config.EnableThumbnail;
        }

        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void OnTaskUpdated(object sender, DownloadTask task)
        {
            Dispatcher.Invoke(() =>
            {
                var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existing != null)
                {
                    existing.UpdateFrom(task);
                }
                else
                {
                    _tasks.Insert(0, task);
                }
            });
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            int active = _tasks.Count(t => t.Status == "Queued" || t.Status == "Downloading");
            int completed = _tasks.Count(t => t.Status == "Completed");
            int failed = _tasks.Count(t => t.Status == "Failed");

            StatsText.Text = $"Active: {active}  |  Completed: {completed}  |  Failed: {failed}";
        }

        private void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            string text = BatchInput.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                ShowMessage("Please enter at least one URL or search term", "Information");
                return;
            }

            UpdateConfigFromUI();

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrEmpty(l))
                           .ToList();

            foreach (var line in lines)
            {
                _downloadManager.AddTask(line);
            }

            BatchInput.Text = "";
            ShowMessage($"✓ Added {lines.Count} track{(lines.Count > 1 ? "s" : "")} to queue", "Success");
        }

        private void DownloadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfigFromUI();

            foreach (var track in PlaylistData.Tracks)
            {
                _downloadManager.AddTask(track);
            }

            ShowMessage($"✓ Added {PlaylistData.Tracks.Count} tracks from playlist", "Success");
        }

        private void UpdateConfigFromUI()
        {
            _config.Format = (FormatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "mp3";
            _config.Bitrate = (QualityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "192";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _config.DownloadFolder = FolderTextBox.Text;
            _config.Format = (FormatComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "mp3";
            _config.Bitrate = (QualityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "192";
            _config.ParallelDownloads = int.Parse((ParallelComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "2");
            _config.MaxRetries = int.Parse((RetryComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "3");
            _config.Numbering = NumberingCheckBox.IsChecked ?? false;
            _config.EnableMetadata = MetadataCheckBox.IsChecked ?? false;
            _config.EnableThumbnail = ThumbnailCheckBox.IsChecked ?? false;

            _config.Save();

            // Restart download manager with new settings
            _downloadManager.Stop();
            _downloadManager.UpdateConfig(_config);
            _downloadManager.Start();

            ShowMessage("✅ Settings saved successfully!", "Success");
        }

        private void ClearQueue_Click(object sender, RoutedEventArgs e)
        {
            var completedOrFailed = _tasks.Where(t => t.Status == "Completed" || t.Status == "Failed").ToList();
            foreach (var task in completedOrFailed)
            {
                _tasks.Remove(task);
            }
            ShowMessage("✓ Queue cleared", "Success");
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                _config.DownloadFolder);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            Process.Start("explorer.exe", folder);
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloader.log");

            if (File.Exists(logPath))
            {
                Process.Start("notepad.exe", logPath);
            }
            else
            {
                ShowMessage("Log file not found.", "Information");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _downloadManager.Stop();
            _config.Save();
            Application.Current.Shutdown();
        }

        private void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK,
                title == "Success" ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _downloadManager.Stop();
            _config.Save();
            base.OnClosing(e);
        }
    }
}
