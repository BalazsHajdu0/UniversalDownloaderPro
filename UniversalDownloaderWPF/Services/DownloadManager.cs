using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalDownloaderWPF.Models;

namespace UniversalDownloaderWPF.Services
{
    public class DownloadManager
    {
        public event EventHandler<DownloadTask> TaskUpdated;

        private AppConfig _config;
        private readonly ConcurrentQueue<DownloadTask> _taskQueue;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;
        private bool _running;
        private int _trackCounter;
        private readonly string _logPath;

        public DownloadManager(AppConfig config)
        {
            _config = config;
            _taskQueue = new ConcurrentQueue<DownloadTask>();
            _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloader.log");
        }

        public void UpdateConfig(AppConfig config)
        {
            _config = config;
        }

        public string AddTask(string query)
        {
            _trackCounter++;
            var task = new DownloadTask
            {
                Id = $"task_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{_trackCounter}",
                Query = query,
                TrackNumber = _trackCounter,
                Status = "Queued"
            };

            _taskQueue.Enqueue(task);
            TaskUpdated?.Invoke(this, task);
            Log($"Task added #{_trackCounter}: {query}");

            return task.Id;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;

            for (int i = 0; i < _config.ParallelDownloads; i++)
            {
                int workerId = i;
                Task.Run(() => WorkerLoop(workerId));
            }

            Log($"Started {_config.ParallelDownloads} workers");
        }

        public void Stop()
        {
            _running = false;
            foreach (var cts in _cancellationTokens.Values)
            {
                cts.Cancel();
            }
            _cancellationTokens.Clear();
            Log("Manager stopped");
        }

        public void CancelTask(string taskId)
        {
            if (_cancellationTokens.TryGetValue(taskId, out var cts))
            {
                cts.Cancel();
                Log($"Task cancelled: {taskId}");
            }
        }

        private async Task WorkerLoop(int workerId)
        {
            while (_running)
            {
                if (_taskQueue.TryDequeue(out var task))
                {
                    var cts = new CancellationTokenSource();
                    _cancellationTokens[task.Id] = cts;

                    await DownloadTask(task, cts.Token);

                    _cancellationTokens.TryRemove(task.Id, out _);
                }
                else
                {
                    await Task.Delay(500);
                }
            }
        }

        private async Task DownloadTask(DownloadTask task, CancellationToken cancellationToken)
        {
            task.Status = "Downloading";
            TaskUpdated?.Invoke(this, task);

            for (int attempt = 0; attempt < _config.MaxRetries; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    task.Status = "Cancelled";
                    TaskUpdated?.Invoke(this, task);
                    return;
                }

                try
                {
                    await PerformDownload(task, cancellationToken);
                    task.Status = "Completed";
                    task.Progress = 100;
                    TaskUpdated?.Invoke(this, task);
                    Log($"✓ Completed: {task.Query}");
                    return;
                }
                catch (OperationCanceledException)
                {
                    task.Status = "Cancelled";
                    TaskUpdated?.Invoke(this, task);
                    return;
                }
                catch (Exception ex)
                {
                    task.Retries = attempt + 1;
                    Log($"✗ Failed (attempt {attempt + 1}/{_config.MaxRetries}): {ex.Message}");

                    if (attempt < _config.MaxRetries - 1)
                    {
                        int waitTime = (int)Math.Pow(2, attempt) * 1000;
                        await Task.Delay(waitTime, cancellationToken);
                    }
                    else
                    {
                        task.Status = "Failed";
                        task.Error = ex.Message.Length > 100 ? ex.Message.Substring(0, 100) : ex.Message;
                        TaskUpdated?.Invoke(this, task);
                    }
                }
            }
        }

        private async Task PerformDownload(DownloadTask task, CancellationToken cancellationToken)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string ytdlpPath = Path.Combine(basePath, "Tools", "yt-dlp.exe");
            string ffmpegPath = Path.Combine(basePath, "Tools");

            if (!File.Exists(ytdlpPath))
            {
                throw new FileNotFoundException("yt-dlp.exe not found in Tools folder");
            }

            // Output folder
            string outputFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                _config.DownloadFolder);
            Directory.CreateDirectory(outputFolder);

            // Filename template
            string template = _config.Numbering
                ? $"track{task.TrackNumber}"
                : "%(title)s";

            // Build arguments
            string format = _config.Format;
            string args;

            if (format == "mp4")
            {
                args = $"--ffmpeg-location \"{ffmpegPath}\" " +
                       $"-f \"bestvideo[ext=mp4][height<=720]+bestaudio[ext=m4a]/best[ext=mp4]/best\" " +
                       $"--merge-output-format mp4 " +
                       $"-o \"{outputFolder}/{template}.%(ext)s\" " +
                       $"--no-warnings --progress ";
            }
            else
            {
                string audioFormat = format == "ogg" ? "vorbis" : format;
                args = $"--ffmpeg-location \"{ffmpegPath}\" " +
                       $"-x --audio-format {audioFormat} " +
                       $"--audio-quality {_config.Bitrate}K " +
                       $"-o \"{outputFolder}/{template}.%(ext)s\" " +
                       $"--no-warnings --progress ";

                if (_config.EnableMetadata)
                {
                    args += "--embed-metadata ";
                }

                if (_config.EnableThumbnail)
                {
                    args += "--embed-thumbnail ";
                }
            }

            // URL or search
            string url = task.Query.Trim();
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = $"ytsearch1:{url}";
            }
            args += $"\"{url}\"";

            // Start process
            var psi = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();

                // Parse progress output
                var outputTask = Task.Run(async () =>
                {
                    using (var reader = process.StandardOutput)
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            ParseProgress(task, line);
                        }
                    }
                }, cancellationToken);

                var errorTask = Task.Run(async () =>
                {
                    using (var reader = process.StandardError)
                    {
                        return await reader.ReadToEndAsync();
                    }
                }, cancellationToken);

                // Wait for process with cancellation
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try { process.Kill(); } catch { }
                        throw new OperationCanceledException();
                    }
                    await Task.Delay(100);
                }

                await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception(string.IsNullOrEmpty(error) ? "Download failed" : error);
                }
            }
        }

        private void ParseProgress(DownloadTask task, string line)
        {
            try
            {
                // Parse: [download]  45.2% of 10.5MiB at 1.2MiB/s ETA 00:05
                var progressMatch = Regex.Match(line, @"(\d+\.?\d*)%");
                if (progressMatch.Success)
                {
                    task.Progress = double.Parse(progressMatch.Groups[1].Value,
                        System.Globalization.CultureInfo.InvariantCulture);
                }

                var speedMatch = Regex.Match(line, @"at\s+([\d.]+\s*[KMG]?i?B/s)");
                if (speedMatch.Success)
                {
                    task.Speed = speedMatch.Groups[1].Value;
                }

                var etaMatch = Regex.Match(line, @"ETA\s+(\d+:\d+)");
                if (etaMatch.Success)
                {
                    task.Eta = etaMatch.Groups[1].Value;
                }

                TaskUpdated?.Invoke(this, task);
            }
            catch { }
        }

        private void Log(string message)
        {
            try
            {
                string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                File.AppendAllText(_logPath, logLine);
            }
            catch { }
        }
    }
}
