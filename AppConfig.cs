using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenCaptureApp
{
    public class CaptureAreaConfig
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;

        [JsonIgnore]
        public Rectangle Rectangle => new Rectangle(X, Y, Width, Height);

        public CaptureAreaConfig() { }

        public CaptureAreaConfig(Rectangle rect)
        {
            X = rect.X;
            Y = rect.Y;
            Width = rect.Width;
            Height = rect.Height;
        }
    }

    public class AppConfig
    {
        public CaptureAreaConfig CaptureArea { get; set; } = new CaptureAreaConfig();
        public bool IsCustomArea { get; set; } = false;
        
        // Delay 1: Interval antar screenshoot (contoh: 10 detik)
        public double ScreenshotIntervalSeconds { get; set; } = 10;

        // Delay 2: Delay setelah screenshoot sebelum menekan tombol (contoh: 2 detik)
        public double PostCaptureDelaySeconds { get; set; } = 2;

        // Tombol yang akan ditekan setelah delay 2 (contoh: "{RIGHT}", "{PGDN}", "{ENTER}", "{SPACE}", dll)
        public string KeyToPress { get; set; } = "{RIGHT}";

        // Folder penyimpanan hasil screenshoot
        public string OutputFolder { get; set; } = "";

        // Penomoran urut mulai dari angka ini (contoh: 1)
        public int StartIndex { get; set; } = 1;

        // Lanjutkan dari nomor terakhir yang ada di folder
        public bool AutoDetectNextNumber { get; set; } = true;

        // Format nama file: {0} akan diganti dengan nomor urut
        public string FileNameFormat { get; set; } = "{0}.png";

        // Jumlah target screenshoot yang akan dilakukan (contoh: 100)
        public int TargetScreenshotCount { get; set; } = 100;

        // Apakah membatasi jumlah screenshoot (jika false = unlimited sampai user stop manual)
        public bool UseTargetCount { get; set; } = true;

        // Minimize saat mulai
        public bool MinimizeOnStart { get; set; } = true;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string GetDefaultConfigPath()
        {
            string localPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            if (File.Exists(localPath)) return localPath;

            string appBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(appBasePath)) return appBasePath;

            return localPath;
        }

        public void Save(string? filePath = null)
        {
            filePath ??= GetDefaultConfigPath();
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        public static AppConfig Load(string? filePath = null)
        {
            filePath ??= GetDefaultConfigPath();
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch
            {
                // Jika error saat membaca, gunakan default
            }

            var defaultConfig = new AppConfig();
            // Default output folder: subfolder "Screenshots" di lokasi aplikasi
            defaultConfig.OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            return defaultConfig;
        }
    }
}
