using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace UniversalDownloaderWPF.Models
{
    [System.Runtime.Serialization.DataContract]
    public class AppConfig
    {
        [System.Runtime.Serialization.DataMember]
        public string DownloadFolder { get; set; } = "Downloads";

        [System.Runtime.Serialization.DataMember]
        public string Format { get; set; } = "mp3";

        [System.Runtime.Serialization.DataMember]
        public bool Numbering { get; set; } = false;

        [System.Runtime.Serialization.DataMember]
        public string FilenameTemplate { get; set; } = "%(title)s";

        [System.Runtime.Serialization.DataMember]
        public int ParallelDownloads { get; set; } = 2;

        [System.Runtime.Serialization.DataMember]
        public int MaxRetries { get; set; } = 3;

        [System.Runtime.Serialization.DataMember]
        public string Bitrate { get; set; } = "192";

        [System.Runtime.Serialization.DataMember]
        public string VideoQuality { get; set; } = "720";

        [System.Runtime.Serialization.DataMember]
        public bool EnableMetadata { get; set; } = false;

        [System.Runtime.Serialization.DataMember]
        public bool EnableThumbnail { get; set; } = false;

        private static string ConfigPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public void Save()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, this);
                    string json = Encoding.UTF8.GetString(stream.ToArray());
                    File.WriteAllText(ConfigPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config save error: {ex.Message}");
            }
        }

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var serializer = new DataContractJsonSerializer(typeof(AppConfig));
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        return (AppConfig)serializer.ReadObject(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config load error: {ex.Message}");
            }
            return new AppConfig();
        }
    }
}
