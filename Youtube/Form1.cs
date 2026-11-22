using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Youtube
{
    public partial class Form1 : Form
    {
        // ==================== ПОЛЯ ====================
        private float titleBarRowHeight;
        private float toolbarRowHeight;
        private bool isYouTubeFullscreen;
        private bool isMiniPlayer = false;
        private Size normalSize;
        private Point normalLocation;
        private FormBorderStyle normalBorderStyle;
        private FormWindowState normalWindowState;
        private double savedZoomFactor = 1.0;

        // Мини-плеер (PiP)
        private Panel pipOverlay;
        private Button pipRestoreBtn, pipCloseBtn, pipMaximizeBtn;
        private Panel pipDragHandle;

        // Скачивание
        private Process _downloadProcess;
        private bool _isDownloading = false;
        private string _currentDownloadLogPath;
        private System.Windows.Forms.Timer _progressTimer;
        private string _lastOpenedFolder;

        // Элементы управления
        private ComboBox cmbQuality;
        private WindowSettings _settings;

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const string HomeUrl = "https://www.youtube.com/";

        // Повноекранний режим YouTube: збережений стан вікна
        private FormWindowState _fsPrevState;
        private Rectangle _fsPrevBounds;
        private bool _fsPrevTopMost;

        private WebView2 webView;
        private Button btnBack, btnForward, btnRefresh, btnHome, btnShare, btnSearch,
                       btnPip, btnVoice, btnDownload, btnDownloadPlaylist, btnDownloadMp3,
                       btnOpenFolder, btnChooseFolder, btnCancelDownload;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private readonly string WebViewDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebViewData");

        // ==================== КОНСТРУКТОР ====================
        public Form1()
        {
            InitializeComponent();

            if (this.Icon != null)
                notifyIcon.Icon = this.Icon;
            else
                notifyIcon.Icon = SystemIcons.Application;

            notifyIcon.Visible = true;

            if (mainLayout.RowStyles.Count >= 2)
            {
                titleBarRowHeight = mainLayout.RowStyles[0].Height;
                toolbarRowHeight = mainLayout.RowStyles[1].Height;
            }

            DoubleBuffered = true;
            KeyPreview = true;

            _settings = LoadSettings();

            btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            btnMaximize.Click += (s, e) => ToggleWindowState();
            btnClose.Click += (s, e) => Close();

            notifyIcon.MouseDoubleClick += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;
                else
                    WindowState = FormWindowState.Minimized;
            };

            notifyIcon.BalloonTipClicked += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_lastOpenedFolder))
                    OpenDownloadsFolder(_lastOpenedFolder);
            };

            titleBar.MouseDown += TitleBar_MouseDown;
            titlePanel.MouseDown += TitleBar_MouseDown;
            iconTitle.MouseDown += TitleBar_MouseDown;
            labelTitle.MouseDown += TitleBar_MouseDown;

            FormClosing += (s, e) =>
            {
                if (isYouTubeFullscreen) SetYouTubeFullscreen(false);
                SaveWindowSettings();
                _progressTimer?.Stop();
                _progressTimer?.Dispose();
                try
                {
                    if (_downloadProcess != null && !_downloadProcess.HasExited)
                        _downloadProcess.Kill(true);
                }
                catch { }
                _downloadProcess?.Dispose();
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
            };

            InitializeInterface();

            UpdateWindowButtonState();
            Resize += (s, e) => UpdateWindowButtonState();
            Load += Form1_Load;

            _ = CheckYtDlpUpdateAsync();
        }

        // ==================== НАСТРОЙКИ ====================
        private WindowSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var s = JsonSerializer.Deserialize<WindowSettings>(json);
                    if (s != null)
                    {
                        if (s.Width > 0 && s.Height > 0)
                        {
                            StartPosition = FormStartPosition.Manual;
                            Location = new Point(s.X, s.Y);
                            Size = new Size(s.Width, s.Height);
                        }
                        if (s.IsMaximized)
                            WindowState = FormWindowState.Maximized;

                        if (string.IsNullOrEmpty(s.DownloadPath))
                            s.DownloadPath = GetDefaultDownloadsPath();
                        if (string.IsNullOrEmpty(s.LastQuality))
                            s.LastQuality = "1080p";

                        return s;
                    }
                }
            }
            catch { }

            return new WindowSettings
            {
                DownloadPath = GetDefaultDownloadsPath(),
                LastQuality = "1080p"
            };
        }

        private void SaveWindowSettings()
        {
            try
            {
                var s = _settings ?? new WindowSettings();
                s.X = WindowState == FormWindowState.Normal ? Location.X : RestoreBounds.Location.X;
                s.Y = WindowState == FormWindowState.Normal ? Location.Y : RestoreBounds.Location.Y;
                s.Width = WindowState == FormWindowState.Normal ? Width : RestoreBounds.Width;
                s.Height = WindowState == FormWindowState.Normal ? Height : RestoreBounds.Height;
                s.IsMaximized = WindowState == FormWindowState.Maximized;
                s.LastUrl = webView?.Source?.ToString() ?? HomeUrl;

                if (string.IsNullOrEmpty(s.DownloadPath))
                    s.DownloadPath = GetDefaultDownloadsPath();
                if (cmbQuality != null && cmbQuality.SelectedItem != null)
                    s.LastQuality = cmbQuality.SelectedItem.ToString();

                if (isMiniPlayer)
                {
                    s.PipX = Location.X;
                    s.PipY = Location.Y;
                    s.PipWidth = Width;
                    s.PipHeight = Height;
                }

                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static string GetDefaultDownloadsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        // ==================== УПРАВЛЕНИЕ ОКНОМ ====================
        private void ToggleWindowState()
        {
            if (WindowState == FormWindowState.Maximized)
                WindowState = FormWindowState.Normal;
            else
            {
                MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
                WindowState = FormWindowState.Maximized;
            }
            UpdateWindowButtonState();
        }

        private void UpdateWindowButtonState()
        {
            if (btnMaximize == null) return;
            bool isMaximized = WindowState == FormWindowState.Maximized;
            btnMaximize.Text = isMaximized ? "❐" : "□";
            btnMaximize.Invalidate();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (e.Clicks >= 2) { ToggleWindowState(); return; }
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        // ==================== ИНТЕРФЕЙС ====================
        private void InitializeInterface()
        {
            btnBack = CreateNavButton("←", "Назад (Alt + ←)");
            btnForward = CreateNavButton("→", "Вперед (Alt + →)");
            btnRefresh = CreateNavButton("↻", "Оновити (F5 / Ctrl + R)");
            btnHome = CreateNavButton("⌂", "Головна YouTube");
            btnShare = CreateNavButton("↗", "Відкрити у браузері");
            btnSearch = CreateNavButton("🔍", "Пошук (Ctrl + L)");
            btnPip = CreateNavButton("⧉", "Міні-плеєр (Ctrl + P)");
            btnVoice = CreateNavButton("🎤", "Голосовий пошук");
            btnDownload = CreateNavButton("⬇", "Скачати це відео (Ctrl + D)");
            btnDownloadPlaylist = CreateNavButton("📃", "Скачати плейлист (Ctrl + Shift + D)");
            btnDownloadMp3 = CreateNavButton("🎵", "Скачати аудіо MP3");
            btnOpenFolder = CreateNavButton("📂", "Відкрити папку (Ctrl + Shift + O)");
            btnChooseFolder = CreateNavButton("📁", "Вибрати папку завантажень");
            btnCancelDownload = CreateNavButton("✖", "Скасувати (Ctrl + Shift + C)");
            btnCancelDownload.Visible = false;

            cmbQuality = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 36, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Width = 80,
                Margin = new Padding(3, 8, 3, 8)
            };
            cmbQuality.Items.AddRange(new object[] { "1080p", "720p", "480p", "360p", "Audio", "Best" });
            cmbQuality.SelectedItem = cmbQuality.Items.Contains(_settings.LastQuality) ? _settings.LastQuality : "1080p";
            cmbQuality.SelectedIndexChanged += (s, e) =>
            {
                _settings.LastQuality = cmbQuality.SelectedItem?.ToString() ?? "1080p";
                SaveWindowSettings();
            };

            // Добавление элементов в FlowLayoutPanel (toolbarPanel)
            toolbarPanel.Controls.Add(btnBack);
            toolbarPanel.Controls.Add(btnForward);
            toolbarPanel.Controls.Add(btnRefresh);
            toolbarPanel.Controls.Add(btnHome);
            toolbarPanel.Controls.Add(btnShare);
            toolbarPanel.Controls.Add(btnSearch);
            toolbarPanel.Controls.Add(btnPip);
            toolbarPanel.Controls.Add(btnVoice);
            toolbarPanel.Controls.Add(cmbQuality);
            toolbarPanel.Controls.Add(btnDownload);
            toolbarPanel.Controls.Add(btnDownloadPlaylist);
            toolbarPanel.Controls.Add(btnDownloadMp3);
            toolbarPanel.Controls.Add(btnOpenFolder);
            toolbarPanel.Controls.Add(btnChooseFolder);
            toolbarPanel.Controls.Add(btnCancelDownload);

            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(20, 21, 25),
                ForeColor = Color.Gainsboro,
                SizingGrip = false,
                Padding = new Padding(8, 2, 8, 2),
                RenderMode = ToolStripRenderMode.Professional
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Готово",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(170, 170, 180)
            };

            progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Width = 180,
                Style = ProgressBarStyle.Continuous
            };

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);

            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.FromArgb(15, 15, 15)
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15),
                Margin = new Padding(0)
            };
            contentPanel.Controls.Add(webView);
            contentPanel.Controls.Add(statusStrip);

            mainLayout.Controls.Add(contentPanel, 0, 2);

            CreatePipOverlay(contentPanel);

            _progressTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _progressTimer.Tick += ProgressTimer_Tick;

            btnBack.Click += (s, e) => GoBack();
            btnForward.Click += (s, e) => GoForward();
            btnRefresh.Click += (s, e) => { RefreshPage(); statusLabel.Text = "Оновлення..."; };
            btnHome.Click += (s, e) => Navigate(HomeUrl);
            btnShare.Click += (s, e) => SharePage();
            btnSearch.Click += (s, e) => ShowSearchBar();
            btnPip.Click += (s, e) => ToggleMiniPlayer();
            btnVoice.Click += async (s, e) => await StartVoiceSearch();
            btnDownload.Click += (s, e) => StartDownload(false, false);
            btnDownloadPlaylist.Click += (s, e) => StartDownload(true, false);
            btnDownloadMp3.Click += (s, e) => StartDownload(false, true);
            btnOpenFolder.Click += (s, e) => OpenDownloadsFolder(GetDownloadPath());
            btnChooseFolder.Click += (s, e) => ChooseDownloadFolder();
            btnCancelDownload.Click += (s, e) => CancelDownload();

            btnGo.Click += (s, e) => PerformSearch();
            txtAddress.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PerformSearch();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    searchPanel.Visible = false;
                    e.SuppressKeyPress = true;
                }
            };

            UpdateNavigationButtons();
        }

        private string GetDownloadPath()
        {
            string p = _settings?.DownloadPath;
            if (string.IsNullOrEmpty(p))
                p = GetDefaultDownloadsPath();
            try { Directory.CreateDirectory(p); } catch { }
            return p;
        }

        private void ChooseDownloadFolder()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Виберіть папку для завантажень";
                dlg.SelectedPath = GetDownloadPath();
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.DownloadPath = dlg.SelectedPath;
                    SaveWindowSettings();
                    statusLabel.Text = "Папка завантажень: " + dlg.SelectedPath;
                }
            }
        }

        // ==================== PIP OVERLAY ====================
        private void CreatePipOverlay(Panel parentPanel)
        {
            pipOverlay = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(220, 20, 20, 24),
                Visible = false
            };

            pipDragHandle = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Cursor = Cursors.SizeAll
            };
            pipDragHandle.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            };

            pipRestoreBtn = CreatePipButton("⧉", "Повернути в звичайний режим");
            pipMaximizeBtn = CreatePipButton("⛶", "Розгорнути на весь екран");
            pipCloseBtn = CreatePipButton("✕", "Закрити міні-плеєр");

            pipCloseBtn.Click += async (s, e) => await ExitMiniPlayer();
            pipMaximizeBtn.Click += (s, e) => ToggleWindowState();
            pipRestoreBtn.Click += async (s, e) => await ExitMiniPlayer();

            pipCloseBtn.Dock = DockStyle.Right;
            pipMaximizeBtn.Dock = DockStyle.Right;
            pipRestoreBtn.Dock = DockStyle.Right;

            pipOverlay.Controls.Add(pipDragHandle);
            pipOverlay.Controls.Add(pipRestoreBtn);
            pipOverlay.Controls.Add(pipMaximizeBtn);
            pipOverlay.Controls.Add(pipCloseBtn);

            parentPanel.Controls.Add(pipOverlay);
            pipOverlay.BringToFront();
        }

        private Button CreatePipButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                Width = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI Symbol", 10F),
                Cursor = Cursors.Hand,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 70);
            new ToolTip { InitialDelay = 400, AutoPopDelay = 4000, ShowAlways = true }.SetToolTip(btn, tooltip);
            return btn;
        }

        // ==================== СКАЧИВАНИЕ ====================
        private string GetQualityFormat()
        {
            string q = cmbQuality?.SelectedItem?.ToString() ?? "1080p";
            switch (q)
            {
                case "1080p":
                    return "bv*[height<=1080]+ba/b[height<=1080]/bv*+ba/b";
                case "720p":
                    return "bv*[height<=720]+ba/b[height<=720]/bv*+ba/b";
                case "480p":
                    return "bv*[height<=480]+ba/b[height<=480]/bv*+ba/b";
                case "360p":
                    return "bv*[height<=360]+ba/b[height<=360]/bv*+ba/b";
                case "Audio":
                    return "ba/b";
                default:
                    return "bv*+ba/b";
            }
        }

        private static string FindFfmpegDir()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Кандидати: поруч із програмою, у підпапках ffmpeg та ffmpeg\bin, у System32
            string[] candidates =
            {
        baseDir,
        Path.Combine(baseDir, "ffmpeg"),
        Path.Combine(baseDir, "ffmpeg", "bin"),
        Path.Combine(baseDir, "bin"),
        Environment.SystemDirectory
    };

            foreach (string dir in candidates)
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, "ffmpeg.exe")))
                        return dir;
                }
                catch { }
            }

            // Шукаємо у змінній середовища PATH
            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    string d = dir.Trim();
                    if (!string.IsNullOrWhiteSpace(d) && File.Exists(Path.Combine(d, "ffmpeg.exe")))
                        return d;
                }
                catch { }
            }

            return null;
        }

        private string BuildYtDlpArgs(string url, string downloadsPath, bool playlist, bool audioOnly)
        {
            string ffmpegDir = FindFfmpegDir();
            bool hasFfmpeg = ffmpegDir != null;

            // Якщо ffmpeg немає — беремо лише готовий єдиний формат (відео+звук разом),
            // щоб не отримати відео без звуку. Інакше — окремі доріжки з склейкою.
            string format;
            if (audioOnly)
                format = "ba/b";
            else if (hasFfmpeg)
                format = GetQualityFormat();
            else
                format = "b";

            string playlistArg = playlist
                ? "--yes-playlist --ignore-errors --no-overwrites"
                : "--no-playlist";

            string ffmpegArg = hasFfmpeg ? $"--ffmpeg-location \"{ffmpegDir}\" " : "";
            string embedArgs = hasFfmpeg ? "--embed-thumbnail --add-metadata " : "";

            string sponsorArg = (_settings.EnableSponsorBlock && hasFfmpeg)
                ? "--sponsorblock-remove sponsor,selfpromo,interaction "
                : "";

            string progressTemplate =
                "--progress-template \"download:%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s\" " +
                "--progress-template \"postprocess:%(progress._percent_str)s\" ";

            string audioArgs = audioOnly
                ? "--extract-audio --audio-format mp3 --audio-quality 0 "
                : (hasFfmpeg ? "--merge-output-format mp4 " : "");

            // Перевага mp4/m4a задається сортуванням, а не жорстким фільтром формату
            string sortArg = audioOnly ? "" : "-S \"res:1080,ext:mp4:m4a\" ";

            return $"-P \"{downloadsPath}\" " +
                   $"-f \"{format}\" " +
                   sortArg +
                   audioArgs +
                   ffmpegArg +
                   embedArgs +
                   sponsorArg +
                   "--newline " +
                   progressTemplate +
                   $"{playlistArg} " +
                   $"-o \"%(title)s.%(ext)s\" " +
                   $"\"{url}\"";
        }


        private static string FindYtDlpPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Кандидати: підпапка yt-dlp, потім поруч із програмою
            string[] candidates =
            {
        Path.Combine(baseDir, "yt-dlp", "yt-dlp.exe"),
        Path.Combine(baseDir, "yt-dlp.exe")
    };

            foreach (string path in candidates)
            {
                try
                {
                    if (File.Exists(path))
                        return path;
                }
                catch { }
            }

            return null;
        }

        private async void StartDownload(bool playlist, bool audioOnly)
        {
            if (_isDownloading)
            {
                MessageBox.Show("Завантаження вже йде.", "Увага", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string url = webView?.Source?.ToString();
            if (!IsValidYouTubeWatchUrl(url))
            {
                MessageBox.Show("Спочатку відкрийте відео на YouTube.", "Скачування", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (playlist && !url.Contains("list="))
            {
                MessageBox.Show("У цьому відео немає плейлиста (параметр list= відсутній).", "Плейлист", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ytDlpPath = FindYtDlpPath();
            if (ytDlpPath == null)
            {
                MessageBox.Show("Файл yt-dlp.exe не знайдено. Покладіть його в папку «yt-dlp» поруч із програмою.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (FindFfmpegDir() == null && !audioOnly)
            {
                var answer = MessageBox.Show(
                    "ffmpeg.exe не знайдено. Без нього відео високої якості буде без звуку, тому буде скачано формат нижчої якості зі звуком.\n\nЩоб скачувати у повній якості, покладіть ffmpeg.exe поруч із програмою.\n\nПродовжити?",
                    "ffmpeg не знайдено", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
            }

            string downloadsPath = GetDownloadPath();
            string args = BuildYtDlpArgs(url, downloadsPath, playlist, audioOnly);

            _currentDownloadLogPath = Path.Combine(downloadsPath, "aureon_download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
            try { File.WriteAllText(_currentDownloadLogPath, ""); } catch { }

            try
            {
                if (_downloadProcess != null)
                {
                    try { _downloadProcess.Dispose(); } catch { }
                    _downloadProcess = null;
                }

                // Запускаємо через cmd.exe /c "yt-dlp.exe ... > log.txt 2>&1"
                // CMD видно (CreateNoWindow = false), вивід yt-dlp пишеться в лог через редирект cmd.
                string cmdArgs = $"/c \"\"{ytDlpPath}\" {args} > \"{_currentDownloadLogPath}\" 2>&1\"";

                _downloadProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = cmdArgs,
                        UseShellExecute = false,
                        CreateNoWindow = false,       // <-- CMD ВИДНО
                        WorkingDirectory = downloadsPath,
                        WindowStyle = ProcessWindowStyle.Normal
                    },
                    EnableRaisingEvents = true
                };

                _downloadProcess.Exited += (s, e) =>
                {
                    int code = -1;
                    try { code = _downloadProcess.ExitCode; } catch { }

                    BeginInvoke(new Action(() =>
                    {
                        _isDownloading = false;
                        _progressTimer?.Stop();
                        btnDownload.Visible = true;
                        btnDownloadPlaylist.Visible = true;
                        btnDownloadMp3.Visible = true;
                        btnCancelDownload.Visible = false;
                        progressBar.Visible = false;
                        progressBar.Value = 0;

                        if (code == 0)
                        {
                            statusLabel.Text = "Завантаження завершено!";
                            _lastOpenedFolder = downloadsPath;
                            notifyIcon.ShowBalloonTip(5000, "AUREON YouTube",
                                "Файл успішно завантажено! Клацніть, щоб відкрити папку.", ToolTipIcon.Info);
                            OpenDownloadsFolder(downloadsPath);
                        }
                        else
                        {
                            statusLabel.Text = "Завантаження перервано або помилка (код " + code + ").";
                            notifyIcon.ShowBalloonTip(5000, "Помилка завантаження",
                                "Код " + code + ". Деталі в CMD або лозі: " + _currentDownloadLogPath,
                                ToolTipIcon.Error);
                        }

                        try { _downloadProcess?.Dispose(); } catch { }
                        _downloadProcess = null;
                    }));
                };

                _isDownloading = true;
                btnDownload.Visible = false;
                btnDownloadPlaylist.Visible = false;
                btnDownloadMp3.Visible = false;
                btnCancelDownload.Visible = true;
                progressBar.Visible = true;
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Continuous;
                statusLabel.Text = playlist ? "Скачування плейлиста... (CMD)" :
                                   (audioOnly ? "Скачування аудіо... (CMD)" : "Скачування відео... (CMD)");

                _downloadProcess.Start();
                _progressTimer.Start();
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                _progressTimer?.Stop();
                btnDownload.Visible = true;
                btnDownloadPlaylist.Visible = true;
                btnDownloadMp3.Visible = true;
                btnCancelDownload.Visible = false;
                progressBar.Visible = false;
                MessageBox.Show("Помилка запуску yt-dlp: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDownloadLogPath) || !File.Exists(_currentDownloadLogPath))
                return;

            try
            {
                string content;
                using (var fs = new FileStream(_currentDownloadLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    fs.Seek(Math.Max(0, fs.Length - 2000), SeekOrigin.Begin);
                    content = sr.ReadToEnd();
                }

                var matches = Regex.Matches(content, @"download:\s*([\d\.]+)%\|([^|]*)\|([^\r\n]*)");
                if (matches.Count > 0)
                {
                    var m = matches[matches.Count - 1];
                    if (double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pct))
                    {
                        progressBar.Value = (int)Math.Min(100, Math.Max(0, pct));
                        string speed = m.Groups[2].Value.Trim();
                        string eta = m.Groups[3].Value.Trim();
                        statusLabel.Text = $"Скачування: {pct:F1}% · {speed} · ETA {eta}";
                    }
                }
            }
            catch { }
        }

        private bool IsValidYouTubeWatchUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
            string host = uri.Host.ToLowerInvariant();
            return host.EndsWith("youtube.com") || host.EndsWith("youtu.be");
        }

        private void OpenDownloadsFolder(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                    Process.Start("explorer.exe", $"\"{folderPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося відкрити папку: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelDownload()
        {
            if (_downloadProcess != null && !_downloadProcess.HasExited)
            {
                try
                {
                    _downloadProcess.Kill(true);
                    statusLabel.Text = "Завантаження скасовано.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не вдалося скасувати: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _isDownloading = false;
                    _progressTimer?.Stop();
                    btnDownload.Visible = true;
                    btnDownloadPlaylist.Visible = true;
                    btnDownloadMp3.Visible = true;
                    btnCancelDownload.Visible = false;
                    progressBar.Visible = false;
                    progressBar.Value = 0;
                }
            }
        }

        private async Task CheckYtDlpUpdateAsync()
        {
            try
            {
                string ytDlpPath = FindYtDlpPath();
                if (ytDlpPath == null) return;

                string localVersion = null;
                var psi = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                using (var p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        localVersion = p.StandardOutput.ReadToEnd().Trim();
                        p.WaitForExit(3000);
                    }
                }

                if (string.IsNullOrEmpty(localVersion)) return;

                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("AureonYouTube/1.0");
                    string json = await http.GetStringAsync("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest");
                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                        {
                            string latest = tag.GetString()?.TrimStart('v') ?? "";
                            if (!string.IsNullOrEmpty(latest) && !string.Equals(latest, localVersion.TrimStart('v'), StringComparison.OrdinalIgnoreCase))
                            {
                                BeginInvoke(new Action(() =>
                                {
                                    statusLabel.Text = $"Доступне оновлення yt-dlp: {latest}";
                                }));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // ==================== МИНИ-ПЛЕЕР ====================
        private async void ToggleMiniPlayer()
        {
            if (!isMiniPlayer)
            {
                if (webView?.Source == null || !webView.Source.ToString().Contains("watch"))
                {
                    MessageBox.Show("Міні-плеєр доступний лише під час перегляду відео.", "Міні-плеєр", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                normalWindowState = WindowState;
                if (WindowState == FormWindowState.Maximized)
                {
                    normalSize = RestoreBounds.Size;
                    normalLocation = RestoreBounds.Location;
                    WindowState = FormWindowState.Normal;
                    await Task.Delay(150);
                }
                else
                {
                    normalSize = Size;
                    normalLocation = Location;
                }

                normalBorderStyle = FormBorderStyle;
                savedZoomFactor = webView?.ZoomFactor ?? 1.0;

                MinimumSize = new Size(320, 180);
                MaximumSize = Size.Empty;
                FormBorderStyle = FormBorderStyle.None;
                TopMost = true;

                Size = new Size(_settings.PipWidth, _settings.PipHeight);

                if (webView != null) webView.ZoomFactor = 1.0;

                var screen = Screen.FromControl(this).WorkingArea;
                if (_settings.PipX >= 0 && _settings.PipY >= 0 &&
                    _settings.PipX + _settings.PipWidth <= screen.Right &&
                    _settings.PipY + _settings.PipHeight <= screen.Bottom)
                {
                    Location = new Point(_settings.PipX, _settings.PipY);
                }
                else
                {
                    Location = new Point(screen.Right - Width - 20, screen.Bottom - Height - 20);
                }

                pipOverlay.Visible = true;
                pipOverlay.BringToFront();

                await Task.Delay(150);
                await RefreshYouTubeLayout();

                isMiniPlayer = true;
                statusLabel.Text = "Міні-плеєр (Ctrl + P / Esc для виходу)";
            }
            else
            {
                await ExitMiniPlayer();
            }
        }

        private async Task ExitMiniPlayer()
        {
            if (webView != null) webView.ZoomFactor = savedZoomFactor;

            pipOverlay.Visible = false;

            FormBorderStyle = normalBorderStyle;
            TopMost = false;
            MinimumSize = Size.Empty;
            Size = normalSize;
            Location = normalLocation;

            if (normalWindowState == FormWindowState.Maximized)
            {
                MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
                WindowState = FormWindowState.Maximized;
            }

            await Task.Delay(150);
            await RefreshYouTubeLayout();

            isMiniPlayer = false;
            statusLabel.Text = "Готово";
        }

        private async Task RefreshYouTubeLayout()
        {
            if (webView?.CoreWebView2 == null) return;
            string script = @"
                window.dispatchEvent(new Event('resize'));
                const player = document.querySelector('.html5-video-player');
                if (player) {
                    player.classList.remove('ytp-small-mode');
                    player.classList.remove('ytp-tiny-mode');
                }
            ";
            try { await webView.CoreWebView2.ExecuteScriptAsync(script); } catch { }
        }

        // ==================== ПРОЧЕЕ ====================
        private async Task StartVoiceSearch()
        {
            if (webView?.CoreWebView2 == null) return;
            string script = @"
                let clicked = false;
                const micSelectors = ['#microphone-button', 'button[aria-label*=""голос""]', 'button[aria-label*=""voice""]', 'button[aria-label*=""mic""]'];
                for (let selector of micSelectors) {
                    const btn = document.querySelector(selector);
                    if (btn) { btn.click(); clicked = true; break; }
                }
                if (!clicked) {
                    const search = document.querySelector('input#search');
                    if (search) search.focus();
                }
            ";
            await webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private void ShowSearchBar()
        {
            searchPanel.Visible = !searchPanel.Visible;
            if (searchPanel.Visible)
            {
                searchPanel.BringToFront();
                txtAddress.Focus();
                txtAddress.SelectAll();
                if (webView?.Source != null)
                    txtAddress.Text = webView.Source.ToString();
            }
        }

        private void PerformSearch()
        {
            string query = txtAddress.Text.Trim();
            if (string.IsNullOrEmpty(query)) return;

            if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                Navigate(query);
            }
            else
            {
                string searchUrl = "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
                Navigate(searchUrl);
            }

            searchPanel.Visible = false;
        }

        private void SharePage()
        {
            try
            {
                if (webView?.Source != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = webView.Source.ToString(),
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося відкрити браузер: " + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Button CreateNavButton(string text, string tooltip)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(36, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(215, 215, 225),
                Font = new Font("Segoe UI Symbol", 11F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 6, 2, 6),
                Padding = new Padding(0),
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 47, 56);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 62, 72);
            new ToolTip { InitialDelay = 400, ReshowDelay = 100, AutoPopDelay = 4000, ShowAlways = true }.SetToolTip(btn, tooltip);
            return btn;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "Ініціалізація WebView2...";

                Directory.CreateDirectory(WebViewDataPath);
                var env = await CoreWebView2Environment.CreateAsync(null, WebViewDataPath);
                await webView.EnsureCoreWebView2Async(env);

                webView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
                webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;

                try { webView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark; } catch { }

                webView.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    if (args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                        args.State = CoreWebView2PermissionState.Allow;
                };

                webView.CoreWebView2.ContainsFullScreenElementChanged += (s, args) =>
                    SetYouTubeFullscreen(webView.CoreWebView2.ContainsFullScreenElement);

                webView.CoreWebView2.NavigationStarting += (s, args) =>
                {
                    statusLabel.Text = "Завантаження...";
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                };

                webView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    statusLabel.Text = args.IsSuccess ? "Готово" : "Помилка: " + args.WebErrorStatus;
                    progressBar.Visible = false;
                    UpdateNavigationButtons();
                };

                webView.CoreWebView2.HistoryChanged += (s, args) => UpdateNavigationButtons();

                webView.CoreWebView2.DocumentTitleChanged += (s, args) =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        string title = webView.CoreWebView2.DocumentTitle;
                        labelTitle.Text = string.IsNullOrEmpty(title) ? "AUREON YouTube" : "AUREON | " + title;
                    }));
                };

                webView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    if (TryGetValidUrl(args.Uri, out Uri uri))
                    {
                        if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
                            webView.CoreWebView2.Navigate(uri.ToString());
                        else
                            Process.Start(new ProcessStartInfo { FileName = uri.ToString(), UseShellExecute = true });
                    }
                };

                string startUrl = HomeUrl;
                if (!string.IsNullOrEmpty(_settings.LastUrl))
                    startUrl = _settings.LastUrl;

                Navigate(startUrl);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "WebView2 не ініціалізовано";
                btnBack.Enabled = btnForward.Enabled = btnRefresh.Enabled = false;
                MessageBox.Show("Не вдалося запустити WebView2.\n\n" + ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Navigate(string url)
        {
            if (webView?.CoreWebView2 != null && TryGetValidUrl(url, out Uri uri))
                webView.CoreWebView2.Navigate(uri.ToString());
        }

        private bool TryGetValidUrl(string value, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            if (!value.Contains("://")) value = "https://" + value;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return false;
            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private void GoBack() { if (webView?.CoreWebView2?.CanGoBack == true) webView.CoreWebView2.GoBack(); }
        private void GoForward() { if (webView?.CoreWebView2?.CanGoForward == true) webView.CoreWebView2.GoForward(); }
        private void RefreshPage() { webView?.CoreWebView2?.Reload(); }

        private void UpdateNavigationButtons()
        {
            if (webView?.CoreWebView2 == null) return;
            btnBack.Enabled = webView.CoreWebView2.CanGoBack;
            btnForward.Enabled = webView.CoreWebView2.CanGoForward;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.L)) { ShowSearchBar(); return true; }
            if (keyData == Keys.F5 || keyData == (Keys.Control | Keys.R)) { RefreshPage(); return true; }
            if (keyData == (Keys.Alt | Keys.Left)) { GoBack(); return true; }
            if (keyData == (Keys.Alt | Keys.Right)) { GoForward(); return true; }
            if (keyData == (Keys.Alt | Keys.Home)) { Navigate(HomeUrl); return true; }
            if (keyData == (Keys.Control | Keys.P)) { ToggleMiniPlayer(); return true; }
            if (keyData == Keys.Escape && isMiniPlayer) { _ = ExitMiniPlayer(); return true; }

            if (keyData == (Keys.Control | Keys.D)) { StartDownload(false, false); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.D)) { StartDownload(true, false); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.O)) { OpenDownloadsFolder(GetDownloadPath()); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.C)) { if (_isDownloading) CancelDownload(); return true; }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SetYouTubeFullscreen(bool fullscreen)
        {
            if (isYouTubeFullscreen == fullscreen) return;
            isYouTubeFullscreen = fullscreen;

            SuspendLayout();
            mainLayout.SuspendLayout();

            try
            {
                if (fullscreen)
                {
                    // Запам'ятовуємо поточний стан вікна
                    _fsPrevState = WindowState;
                    _fsPrevBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                    _fsPrevTopMost = TopMost;

                    // Монітор, на якому зараз знаходиться вікно
                    Rectangle screenBounds = Screen.FromHandle(Handle).Bounds;

                    titleBar.Visible = false;
                    topPanel.Visible = false;
                    statusStrip.Visible = false;
                    mainLayout.RowStyles[0].Height = 0;
                    mainLayout.RowStyles[1].Height = 0;

                    // Розтягуємо на весь екран (разом із панеллю завдань)
                    if (WindowState != FormWindowState.Normal)
                        WindowState = FormWindowState.Normal;
                    MinimumSize = Size.Empty;
                    MaximumSize = Size.Empty;
                    Bounds = screenBounds;
                    TopMost = true;
                }
                else
                {
                    // Повертаємо звичайний вигляд
                    TopMost = _fsPrevTopMost;
                    Bounds = _fsPrevBounds;
                    if (_fsPrevState == FormWindowState.Maximized)
                    {
                        MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
                        WindowState = FormWindowState.Maximized;
                    }

                    titleBar.Visible = true;
                    topPanel.Visible = true;
                    statusStrip.Visible = true;
                    mainLayout.RowStyles[0].Height = titleBarRowHeight;
                    mainLayout.RowStyles[1].Height = toolbarRowHeight;
                    UpdateWindowButtonState();
                }
            }
            finally
            {
                mainLayout.ResumeLayout(true);
                ResumeLayout(true);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal && !isMiniPlayer && !isYouTubeFullscreen)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    int border = Math.Max(6, (int)(8 * DeviceDpi / 96.0));
                    int x = (short)(m.LParam.ToInt64() & 0xFFFF);
                    int y = (short)((m.LParam.ToInt64() >> 16) & 0xFFFF);
                    Point cursor = PointToClient(new Point(x, y));

                    bool left = cursor.X <= border;
                    bool right = cursor.X >= ClientSize.Width - border;
                    bool top = cursor.Y <= border;
                    bool bottom = cursor.Y >= ClientSize.Height - border;

                    if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    public class WindowSettings
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsMaximized { get; set; }
        public string LastUrl { get; set; } = "https://www.youtube.com/";

        public int PipX { get; set; } = -1;
        public int PipY { get; set; } = -1;
        public int PipWidth { get; set; } = 640;
        public int PipHeight { get; set; } = 480;

        public string DownloadPath { get; set; } = "";
        public string LastQuality { get; set; } = "1080p";
        public bool EnableSponsorBlock { get; set; } = false;
    }
}