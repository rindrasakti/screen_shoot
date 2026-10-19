using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenCaptureApp
{
    public partial class Form1 : Form
    {
        // P/Invoke untuk Hotkey Global (F9)
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9001;
        private const int WM_HOTKEY = 0x0312;

        private AppConfig _config = new AppConfig();
        private CancellationTokenSource? _captureCts;
        private bool _isRunning = false;
        private int _currentImageIndex = 1;

        // UI Controls
        private Label _lblAreaInfo = null!;
        private Button _btnSelectArea = null!;
        private Button _btnFullScreen = null!;
        private Button _btnTestCapture = null!;

        private NumericUpDown _numInterval = null!;
        private NumericUpDown _numPostDelay = null!;
        private ComboBox _cmbKey = null!;
        private NumericUpDown _numTargetCount = null!;
        private CheckBox _chkUseTargetCount = null!;

        private TextBox _txtFolder = null!;
        private Button _btnBrowseFolder = null!;
        private Button _btnOpenFolder = null!;
        private NumericUpDown _numStartNumber = null!;
        private CheckBox _chkAutoDetectNumber = null!;
        private CheckBox _chkMinimizeOnStart = null!;

        private Button _btnStart = null!;
        private Button _btnStop = null!;
        private Label _lblStatus = null!;
        private ListBox _lstLog = null!;
        private Button _btnClearLog = null!;
        private Button _btnSaveConfig = null!;

        public Form1()
        {
            InitializeComponent();
            SetApplicationIcon();
            SetupCustomUI();
            LoadConfiguration();
            RegisterGlobalHotkey();
        }

        private void SetApplicationIcon()
        {
            try
            {
                // Cek app.ico terlebih dahulu
                string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (!File.Exists(icoPath))
                {
                    icoPath = Path.Combine(Directory.GetCurrentDirectory(), "app.ico");
                }

                if (File.Exists(icoPath))
                {
                    this.Icon = new Icon(icoPath);
                    return;
                }

                // Fallback langsung menggunakan icon.png
                string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
                if (!File.Exists(pngPath))
                {
                    pngPath = Path.Combine(Directory.GetCurrentDirectory(), "icon.png");
                }

                if (File.Exists(pngPath))
                {
                    using var bmp = new Bitmap(pngPath);
                    IntPtr hIcon = bmp.GetHicon();
                    this.Icon = Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                // Abaikan jika icon gagal dimuat
            }
        }

        private void RegisterGlobalHotkey()
        {
            try
            {
                // Register F9 sebagai hotkey global
                RegisterHotKey(this.Handle, HOTKEY_ID, 0, (int)Keys.F9);
            }
            catch (Exception ex)
            {
                AddLog($"[PERINGATAN] Gagal mendaftarkan hotkey F9: {ex.Message}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // Toggle Start / Stop via F9
                if (_isRunning)
                {
                    StopCapture();
                }
                else
                {
                    StartCapture();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isRunning)
            {
                StopCapture();
            }

            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
            }
            catch { }

            SaveCurrentUIToConfig();
            base.OnFormClosing(e);
        }

        private void SetupCustomUI()
        {
            this.Text = "Auto Screen Capture & Key Presser";
            this.Size = new Size(680, 825);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(246, 248, 250);

            int yOffset = 15;

            // ================== GROUP 1: AREA SELEKSI LAYAR ==================
            var grpArea = new GroupBox
            {
                Text = " 1. Area Tangkapan Layar (Screen Region) ",
                Location = new Point(20, yOffset),
                Size = new Size(625, 115),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            _lblAreaInfo = new Label
            {
                Location = new Point(15, 28),
                Size = new Size(595, 32),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                Text = "Area Saat Ini: Full Screen"
            };

            _btnSelectArea = new Button
            {
                Text = "🖱️ Pilih Area Layar (Drag)",
                Location = new Point(15, 68),
                Size = new Size(200, 34),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSelectArea.FlatAppearance.BorderSize = 0;
            _btnSelectArea.Click += BtnSelectArea_Click;

            _btnFullScreen = new Button
            {
                Text = "🖥️ Layar Penuh",
                Location = new Point(225, 68),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(230, 235, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnFullScreen.FlatAppearance.BorderSize = 0;
            _btnFullScreen.Click += BtnFullScreen_Click;

            _btnTestCapture = new Button
            {
                Text = "📸 Tes 1x Screenshot",
                Location = new Point(365, 68),
                Size = new Size(160, 34),
                BackColor = Color.FromArgb(230, 235, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnTestCapture.FlatAppearance.BorderSize = 0;
            _btnTestCapture.Click += BtnTestCapture_Click;

            grpArea.Controls.AddRange(new Control[] { _lblAreaInfo, _btnSelectArea, _btnFullScreen, _btnTestCapture });
            this.Controls.Add(grpArea);

            yOffset += 125;

            // ================== GROUP 2: DELAY & TOMBOL ==================
            var grpDelay = new GroupBox
            {
                Text = " 2. Pengaturan Waktu, Tombol Aksi & Target Jumlah ",
                Location = new Point(20, yOffset),
                Size = new Size(625, 172),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var lblInt = new Label
            {
                Text = "Interval Screenshoot (Detik):",
                Location = new Point(15, 32),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _numInterval = new NumericUpDown
            {
                Location = new Point(230, 30),
                Size = new Size(90, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 86400,
                Value = 10
            };

            var lblPost = new Label
            {
                Text = "Delay Setelah Screenshoot (Detik):",
                Location = new Point(15, 64),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _numPostDelay = new NumericUpDown
            {
                Location = new Point(230, 62),
                Size = new Size(90, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Minimum = 0,
                Maximum = 86400,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Value = 2
            };

            var lblKey = new Label
            {
                Text = "Tombol Yang Ditekan:",
                Location = new Point(15, 96),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _cmbKey = new ComboBox
            {
                Location = new Point(230, 93),
                Size = new Size(240, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            _cmbKey.Items.AddRange(new object[]
            {
                "{RIGHT} - Panah Kanan (Next Slide / Page)",
                "{PGDN} - Page Down",
                "{ENTER} - Enter",
                "{SPACE} - Spasi",
                "{DOWN} - Panah Bawah",
                "{LEFT} - Panah Kiri",
                "{TAB} - Tab",
                "NONE - Jangan Tekan Tombol Apapun"
            });
            _cmbKey.SelectedIndex = 0;

            var lblKeyHint = new Label
            {
                Text = "(Bisa diketik custom)",
                Location = new Point(480, 96),
                Size = new Size(135, 20),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            var lblTarget = new Label
            {
                Text = "Jumlah Target Screenshoot:",
                Location = new Point(15, 130),
                Size = new Size(210, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _numTargetCount = new NumericUpDown
            {
                Location = new Point(230, 128),
                Size = new Size(90, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 999999,
                Value = 100
            };

            _chkUseTargetCount = new CheckBox
            {
                Text = "Batasi jumlah (Muncul alert 'Job Done' saat tercapai)",
                Location = new Point(330, 129),
                Size = new Size(285, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Checked = true
            };
            _chkUseTargetCount.CheckedChanged += (s, e) =>
            {
                _numTargetCount.Enabled = _chkUseTargetCount.Checked;
            };

            grpDelay.Controls.AddRange(new Control[] { 
                lblInt, _numInterval, 
                lblPost, _numPostDelay, 
                lblKey, _cmbKey, lblKeyHint,
                lblTarget, _numTargetCount, _chkUseTargetCount 
            });
            this.Controls.Add(grpDelay);

            yOffset += 182;

            // ================== GROUP 3: PENYIMPANAN ==================
            var grpStorage = new GroupBox
            {
                Text = " 3. Folder Penyimpanan & Penomoran (1 .. N) ",
                Location = new Point(20, yOffset),
                Size = new Size(625, 135),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var lblFolder = new Label
            {
                Text = "Folder Tujuan:",
                Location = new Point(15, 30),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _txtFolder = new TextBox
            {
                Location = new Point(120, 27),
                Size = new Size(350, 25),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _btnBrowseFolder = new Button
            {
                Text = "📁 Browse...",
                Location = new Point(480, 25),
                Size = new Size(80, 28),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnBrowseFolder.Click += BtnBrowseFolder_Click;

            _btnOpenFolder = new Button
            {
                Text = "📂",
                Location = new Point(568, 25),
                Size = new Size(42, 28),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnOpenFolder.Click += (s, e) => OpenOutputFolder();

            var lblStartNum = new Label
            {
                Text = "Mulai dari Angka:",
                Location = new Point(15, 65),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _numStartNumber = new NumericUpDown
            {
                Location = new Point(130, 63),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Minimum = 1,
                Maximum = 999999,
                Value = 1
            };

            _chkAutoDetectNumber = new CheckBox
            {
                Text = "Otomatis lanjutkan dari nomor file terbesar di folder",
                Location = new Point(230, 64),
                Size = new Size(370, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Checked = true
            };

            _chkMinimizeOnStart = new CheckBox
            {
                Text = "Minimize aplikasi saat proses screenshot dimulai (agar tidak menutupi layar)",
                Location = new Point(15, 98),
                Size = new Size(480, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Checked = true
            };

            grpStorage.Controls.AddRange(new Control[] { lblFolder, _txtFolder, _btnBrowseFolder, _btnOpenFolder, lblStartNum, _numStartNumber, _chkAutoDetectNumber, _chkMinimizeOnStart });
            this.Controls.Add(grpStorage);

            yOffset += 145;

            // ================== GROUP 4: KONTROL START / STOP ==================
            _btnStart = new Button
            {
                Text = "▶ MULAI SCREENSHOT (F9)",
                Location = new Point(20, yOffset),
                Size = new Size(250, 48),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Click += (s, e) => StartCapture();

            _btnStop = new Button
            {
                Text = "⏹ BERHENTI (F9)",
                Location = new Point(280, yOffset),
                Size = new Size(190, 48),
                BackColor = Color.FromArgb(209, 36, 47),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnStop.FlatAppearance.BorderSize = 0;
            _btnStop.Click += (s, e) => StopCapture();

            _btnSaveConfig = new Button
            {
                Text = "💾 Simpan Config",
                Location = new Point(480, yOffset),
                Size = new Size(165, 48),
                BackColor = Color.FromArgb(230, 235, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSaveConfig.FlatAppearance.BorderSize = 0;
            _btnSaveConfig.Click += (s, e) =>
            {
                SaveCurrentUIToConfig();
                _config.Save();
                AddLog("Konfigurasi berhasil disimpan ke config.json.");
                MessageBox.Show("Konfigurasi berhasil disimpan ke config.json!", "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            this.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnSaveConfig });

            yOffset += 56;

            // ================== STATUS & LOG ==================
            _lblStatus = new Label
            {
                Text = "Status: Siap. Tekan Mulai atau F9 untuk menjalankan.",
                Location = new Point(20, yOffset),
                Size = new Size(540, 24),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 100, 200)
            };

            _btnClearLog = new Button
            {
                Text = "Clear Log",
                Location = new Point(565, yOffset),
                Size = new Size(80, 24),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            _btnClearLog.Click += (s, e) => _lstLog.Items.Clear();

            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnClearLog);

            yOffset += 28;

            _lstLog = new ListBox
            {
                Location = new Point(20, yOffset),
                Size = new Size(625, 140),
                Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                BackColor = Color.White
            };
            this.Controls.Add(_lstLog);
        }

        private void LoadConfiguration()
        {
            _config = AppConfig.Load();

            // Populate UI from config
            if (_config.IsCustomArea && _config.CaptureArea.Width > 0 && _config.CaptureArea.Height > 0)
            {
                _lblAreaInfo.Text = $"Area Seleksi: X={_config.CaptureArea.X}, Y={_config.CaptureArea.Y}, " +
                                    $"Lebar={_config.CaptureArea.Width}px, Tinggi={_config.CaptureArea.Height}px";
            }
            else
            {
                var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                _lblAreaInfo.Text = $"Area Saat Ini: Layar Penuh ({screen.Width} x {screen.Height} px)";
            }

            _numInterval.Value = Math.Clamp((decimal)_config.ScreenshotIntervalSeconds, _numInterval.Minimum, _numInterval.Maximum);
            _numPostDelay.Value = Math.Clamp((decimal)_config.PostCaptureDelaySeconds, _numPostDelay.Minimum, _numPostDelay.Maximum);

            SetKeyComboBoxValue(_config.KeyToPress);

            if (string.IsNullOrWhiteSpace(_config.OutputFolder))
            {
                _config.OutputFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            }
            _txtFolder.Text = _config.OutputFolder;
            _numStartNumber.Value = Math.Clamp(_config.StartIndex, _numStartNumber.Minimum, _numStartNumber.Maximum);
            _chkAutoDetectNumber.Checked = _config.AutoDetectNextNumber;
            _chkMinimizeOnStart.Checked = _config.MinimizeOnStart;

            _numTargetCount.Value = Math.Clamp(_config.TargetScreenshotCount, _numTargetCount.Minimum, _numTargetCount.Maximum);
            _chkUseTargetCount.Checked = _config.UseTargetCount;
            _numTargetCount.Enabled = _config.UseTargetCount;

            AddLog($"Konfigurasi dimuat dari '{AppConfig.GetDefaultConfigPath()}'.");
        }

        private void SetKeyComboBoxValue(string key)
        {
            for (int i = 0; i < _cmbKey.Items.Count; i++)
            {
                string item = _cmbKey.Items[i]?.ToString() ?? "";
                if (item.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    _cmbKey.SelectedIndex = i;
                    return;
                }
            }
            _cmbKey.Text = key;
        }

        private string GetSelectedKey()
        {
            string text = _cmbKey.Text.Trim();
            if (string.IsNullOrEmpty(text) || text.StartsWith("NONE", StringComparison.OrdinalIgnoreCase))
            {
                return "NONE";
            }

            // Jika format "{KEY} - Description", ambil bagian {KEY}
            if (text.Contains(" - "))
            {
                return text.Substring(0, text.IndexOf(" - ")).Trim();
            }

            return text;
        }

        private void SaveCurrentUIToConfig()
        {
            _config.ScreenshotIntervalSeconds = (double)_numInterval.Value;
            _config.PostCaptureDelaySeconds = (double)_numPostDelay.Value;
            _config.KeyToPress = GetSelectedKey();
            _config.OutputFolder = _txtFolder.Text.Trim();
            _config.StartIndex = (int)_numStartNumber.Value;
            _config.AutoDetectNextNumber = _chkAutoDetectNumber.Checked;
            _config.MinimizeOnStart = _chkMinimizeOnStart.Checked;
            _config.TargetScreenshotCount = (int)_numTargetCount.Value;
            _config.UseTargetCount = _chkUseTargetCount.Checked;
        }

        private void BtnSelectArea_Click(object? sender, EventArgs e)
        {
            // Buka overlay untuk drag area layar
            using var regionForm = new RegionSelectForm();
            if (regionForm.ShowDialog() == DialogResult.OK)
            {
                var rect = regionForm.SelectedRectangle;
                _config.CaptureArea = new CaptureAreaConfig(rect);
                _config.IsCustomArea = true;

                _lblAreaInfo.Text = $"Area Seleksi: X={rect.X}, Y={rect.Y}, " +
                                    $"Lebar={rect.Width}px, Tinggi={rect.Height}px";

                // Simpan otomatis ke JSON sesuai requirement #1
                SaveCurrentUIToConfig();
                _config.Save();

                AddLog($"Area disimpan ke JSON: X={rect.X}, Y={rect.Y}, W={rect.Width}, H={rect.Height}");
                MessageBox.Show($"Area layar berhasil dipilih & disimpan ke config.json!\n\nUkuran: {rect.Width} x {rect.Height} px\nPosisi: ({rect.X}, {rect.Y})",
                    "Area Disimpan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnFullScreen_Click(object? sender, EventArgs e)
        {
            var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            _config.CaptureArea = new CaptureAreaConfig(screen);
            _config.IsCustomArea = false;
            _lblAreaInfo.Text = $"Area Saat Ini: Layar Penuh ({screen.Width} x {screen.Height} px)";

            SaveCurrentUIToConfig();
            _config.Save();
            AddLog("Area diatur ke Layar Penuh dan disimpan ke config.json.");
        }

        private void BtnBrowseFolder_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = "Pilih folder untuk menyimpan hasil screenshoot:";
            fbd.SelectedPath = Directory.Exists(_txtFolder.Text) ? _txtFolder.Text : AppDomain.CurrentDomain.BaseDirectory;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _txtFolder.Text = fbd.SelectedPath;
                SaveCurrentUIToConfig();
                _config.Save();
            }
        }

        private void OpenOutputFolder()
        {
            string folder = _txtFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuka folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestCapture_Click(object? sender, EventArgs e)
        {
            try
            {
                string folder = EnsureOutputFolder();
                Rectangle captureRect = GetCurrentCaptureRectangle();

                string testFile = Path.Combine(folder, "test_capture.png");
                CaptureScreen(captureRect, testFile);

                AddLog($"Tes screenshot berhasil disimpan: {testFile}");
                var result = MessageBox.Show($"Screenshot tersimpan di:\n{testFile}\n\nApakah ingin membuka gambarnya sekarang?",
                    "Tes Screenshot Berhasil", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = testFile, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Tes screenshot gagal: {ex.Message}");
                MessageBox.Show($"Gagal mengambil tes screenshot: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string EnsureOutputFolder()
        {
            string folder = _txtFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                _txtFolder.Text = folder;
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }

        private Rectangle GetCurrentCaptureRectangle()
        {
            if (_config.IsCustomArea && _config.CaptureArea.Width > 0 && _config.CaptureArea.Height > 0)
            {
                return _config.CaptureArea.Rectangle;
            }
            return Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        }

        private int DetermineNextImageIndex(string folder)
        {
            if (!_chkAutoDetectNumber.Checked)
            {
                return (int)_numStartNumber.Value;
            }

            int maxIndex = 0;
            try
            {
                var files = Directory.GetFiles(folder, "*.png");
                foreach (var file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (int.TryParse(name, out int num))
                    {
                        if (num > maxIndex) maxIndex = num;
                    }
                }
            }
            catch { }

            return maxIndex > 0 ? maxIndex + 1 : (int)_numStartNumber.Value;
        }

        private void StartCapture()
        {
            if (_isRunning) return;

            SaveCurrentUIToConfig();
            _config.Save();

            string folder = EnsureOutputFolder();
            _currentImageIndex = DetermineNextImageIndex(folder);

            _captureCts = new CancellationTokenSource();
            _isRunning = true;

            SetControlsRunningState(true);
            AddLog($"=== PROSES AUTO SCREENSHOT DIMULAI ===");
            AddLog($"Nomor urut awal: {_currentImageIndex}");
            AddLog($"Folder: {folder}");
            AddLog($"Interval: {_config.ScreenshotIntervalSeconds}s, Delay Tombol: {_config.PostCaptureDelaySeconds}s, Tombol: '{_config.KeyToPress}'");
            if (_config.UseTargetCount)
            {
                AddLog($"Target jumlah: {_config.TargetScreenshotCount} screenshot (Alert 'Job Done' akan muncul setelah selesai).");
            }
            else
            {
                AddLog("Target jumlah: Tanpa batas (Unlimited).");
            }

            if (_config.MinimizeOnStart)
            {
                this.WindowState = FormWindowState.Minimized;
            }

            // Jalankan background loop
            Task.Run(() => CaptureLoopAsync(_captureCts.Token));
        }

        private void StopCapture()
        {
            if (!_isRunning) return;

            _captureCts?.Cancel();
            _isRunning = false;

            SetControlsRunningState(false);
            _lblStatus.Text = "Status: Berhenti.";
            AddLog("=== PROSES AUTO SCREENSHOT DIHENTIKAN ===");

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            }
        }

        private void OnJobCompleted(int totalCaptured)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnJobCompleted(totalCaptured)));
                return;
            }

            _captureCts?.Cancel();
            _isRunning = false;
            SetControlsRunningState(false);

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.BringToFront();
            this.Activate();

            _lblStatus.Text = $"Status: Job Done! ({totalCaptured} screenshot berhasil diambil)";
            AddLog($"=== [JOB DONE] Target {totalCaptured} screenshot telah selesai diambil! ===");

            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }

            MessageBox.Show(
                $"🎉 Job Done!\n\nTarget {totalCaptured} screenshot telah berhasil diambil dan disimpan ke folder:\n{_config.OutputFolder}",
                "Job Done",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetControlsRunningState(bool running)
        {
            _btnStart.Enabled = !running;
            _btnStop.Enabled = running;
            _btnSelectArea.Enabled = !running;
            _btnFullScreen.Enabled = !running;
            _btnTestCapture.Enabled = !running;
            _btnBrowseFolder.Enabled = !running;
            _numInterval.Enabled = !running;
            _numPostDelay.Enabled = !running;
            _cmbKey.Enabled = !running;
            _numStartNumber.Enabled = !running;
            _chkAutoDetectNumber.Enabled = !running;
            _chkMinimizeOnStart.Enabled = !running;
            _btnSaveConfig.Enabled = !running;
            _chkUseTargetCount.Enabled = !running;
            _numTargetCount.Enabled = !running && _chkUseTargetCount.Checked;
        }

        private async Task CaptureLoopAsync(CancellationToken token)
        {
            var folder = _config.OutputFolder;
            var keyToPress = _config.KeyToPress;
            var interval = _config.ScreenshotIntervalSeconds;
            var postDelay = _config.PostCaptureDelaySeconds;
            var captureRect = GetCurrentCaptureRectangle();
            int sessionCapturedCount = 0;
            int targetCount = _config.TargetScreenshotCount;
            bool useTarget = _config.UseTargetCount;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 1. Hitung mundur interval screenshoot
                    int totalSeconds = (int)Math.Ceiling(interval);
                    for (int s = totalSeconds; s > 0; s--)
                    {
                        if (token.IsCancellationRequested) return;

                        string progressInfo = useTarget ? $" [{sessionCapturedCount + 1}/{targetCount}]" : "";
                        UpdateStatusOnUI($"Screenshot #{_currentImageIndex}{progressInfo} dalam {s} detik... (Tekan F9 untuk Stop)");
                        await Task.Delay(1000, token);
                    }

                    if (token.IsCancellationRequested) return;

                    // 2. Ambil screenshoot dan simpan berdasarkan nomor urut 1 .. N
                    string fileName = $"{_currentImageIndex}.png";
                    string filePath = Path.Combine(folder, fileName);

                    UpdateStatusOnUI($"Mengambil screenshot #{_currentImageIndex}...");
                    CaptureScreen(captureRect, filePath);

                    sessionCapturedCount++;
                    string targetProgress = useTarget ? $" ({sessionCapturedCount}/{targetCount})" : "";
                    AddLogOnUI($"[SCREENSHOT] Berhasil disimpan -> '{fileName}' ({captureRect.Width}x{captureRect.Height}){targetProgress}");

                    int capturedIndex = _currentImageIndex;
                    _currentImageIndex++;

                    // Periksa jika target screenshot tercapai
                    if (useTarget && sessionCapturedCount >= targetCount)
                    {
                        OnJobCompleted(sessionCapturedCount);
                        break;
                    }

                    // 3. Delay setelah screenshoot sebelum menekan tombol
                    if (postDelay > 0)
                    {
                        UpdateStatusOnUI($"Screenshot #{capturedIndex} selesai. Menunggu delay {postDelay}s sebelum tombol...");
                        int postMs = (int)(postDelay * 1000);
                        await Task.Delay(postMs, token);
                    }

                    if (token.IsCancellationRequested) return;

                    // 4. Tekan tombol aksi
                    if (!string.IsNullOrEmpty(keyToPress) && !keyToPress.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateStatusOnUI($"Mengirim tombol '{keyToPress}'...");
                        SendKeyPress(keyToPress);
                        AddLogOnUI($"[KEY] Tombol '{keyToPress}' ditekan.");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AddLogOnUI($"[ERROR] Terjadi kesalahan: {ex.Message}");
                    await Task.Delay(2000, token);
                }
            }
        }

        private void CaptureScreen(Rectangle rect, string filePath)
        {
            // Ambil gambar sesuai ukuran rectangle
            using var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            }
            bitmap.Save(filePath, ImageFormat.Png);
        }

        private void SendKeyPress(string key)
        {
            try
            {
                // Kirim tombol menggunakan SendKeys
                SendKeys.SendWait(key);
            }
            catch (Exception ex)
            {
                AddLogOnUI($"[ERROR] Gagal mengirim tombol '{key}': {ex.Message}");
            }
        }

        private void UpdateStatusOnUI(string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => _lblStatus.Text = status));
            }
            else
            {
                _lblStatus.Text = status;
            }
        }

        private void AddLogOnUI(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AddLog(message)));
            }
            else
            {
                AddLog(message);
            }
        }

        private void AddLog(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"[{time}] {message}";
            _lstLog.Items.Add(entry);
            _lstLog.TopIndex = _lstLog.Items.Count - 1;
        }
    }
}
