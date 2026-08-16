using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VisionFramework.App.ViewModels;
using VisionFramework.Core.Configuration;
using VisionFramework.UI.Controls;
using VisionFramework.UI.Views;

namespace VisionFramework.App
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;
        private PlcConfig _plcConfig = new PlcConfig();
        private CameraConfig _cameraConfig = new CameraConfig();
        private SaveImageConfig _saveImageConfig = new SaveImageConfig();
        private bool _isLoggedIn = false;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel(DisplayControl);
            DataContext = _vm;
            InitStatusIndicators();
            UpdateLoginDisplay();
        }

        // ═══ 登录验证 ═══
        private bool RequireLogin()
        {
            if (_isLoggedIn) return true;

            var login = new LoginWindow { Owner = this };
            if (login.ShowDialog() == true && login.LoginSuccess)
            {
                _isLoggedIn = true;
                UpdateLoginDisplay();
                _vm?.Log("用户已登录: admin");
                return true;
            }
            return false;
        }

        private void UpdateLoginDisplay()
        {
            TblUser.Text = _isLoggedIn ? "admin" : "未登录";
            TblUser.Foreground = _isLoggedIn
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC))
                : System.Windows.Media.Brushes.Gray;
        }

        // ═══ 状态指示灯初始化 ═══
        private void InitStatusIndicators()
        {
            LightPlc.SetState(LightState.Red);
            LightCamera.SetState(LightState.Red);
            LightHeartbeat.SetState(LightState.Off);
            LightPing.SetState(LightState.Off);
            UpdateDiskInfo();

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (s, e) => UpdateDiskInfo();
            timer.Start();
        }

        private void UpdateDiskInfo()
        {
            try
            {
                string path = _saveImageConfig.SavePath;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(Path.GetPathRoot(path)))
                {
                    var drive = new DriveInfo(Path.GetPathRoot(path));
                    long totalGB = drive.TotalSize / (1024 * 1024 * 1024);
                    long freeGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
                    double usagePct = (double)(totalGB - freeGB) / totalGB * 100;
                    TblDiskInfo.Text = $"{freeGB}GB / {totalGB}GB ({usagePct:F0}%)";
                    TblDiskInfo.Foreground = usagePct > _saveImageConfig.MaxDiskUsagePercent
                        ? System.Windows.Media.Brushes.Red
                        : System.Windows.Media.Brushes.Coral;
                }
                else
                {
                    TblDiskInfo.Text = "路径无效";
                }
            }
            catch
            {
                TblDiskInfo.Text = "--";
            }
        }

        // ═══ 配置弹窗（需登录） ═══
        private void BtnPlcConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new PlcConfigWindow(_plcConfig);
            if (dlg.ShowDialog() == true)
            {
                _plcConfig = dlg.Config;
                _vm?.Log($"PLC 配置已更新: {_plcConfig.IpAddress}:{_plcConfig.Port}");
            }
        }

        private void BtnCameraConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new CameraConfigWindow(_cameraConfig);
            if (dlg.ShowDialog() == true)
            {
                _cameraConfig = dlg.Config;
                _vm?.Log($"相机配置已更新: {_cameraConfig.CameraType} @ {_cameraConfig.ConnectionString}");
            }
        }

        private void BtnSaveImageConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new SaveImageConfigWindow(_saveImageConfig);
            if (dlg.ShowDialog() == true)
            {
                _saveImageConfig = dlg.Config;
                UpdateDiskInfo();
                _vm?.Log($"存图配置已更新: {_saveImageConfig.SavePath} ({_saveImageConfig.ImageFormat})");
            }
        }

        private void BtnPlcMonitor_Click(object sender, RoutedEventArgs e)
        {
            var monitor = new PlcMonitorWindow { Owner = this };
            monitor.Show();
        }

        private void BtnUserSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new UserSettingsWindow("admin") { Owner = this };
            dlg.ShowDialog();
        }

        private void BtnSysInfo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SystemInfoWindow { Owner = this };
            dlg.ShowDialog();
        }

        // ═══ 进入程序（打开 VisionPro） ═══
        private void BtnOpenVpp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string vppPath = _vm?.CurrentVppPath;
                var cognexDir = @"C:\Program Files (x86)\Cognex\VisionPro\bin";
                string exePath = Path.Combine(cognexDir, "QuickBuild.exe");

                if (!File.Exists(exePath))
                {
                    foreach (var candidate in new[]
                    {
                        @"C:\Program Files\Cognex\VisionPro\bin\QuickBuild.exe",
                        @"C:\Program Files (x86)\Cognex\VisionPro\bin\CogJobManager.exe"
                    })
                    {
                        if (File.Exists(candidate)) { exePath = candidate; break; }
                    }
                }

                if (File.Exists(exePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    };
                    if (!string.IsNullOrEmpty(vppPath) && File.Exists(vppPath))
                        psi.Arguments = $"\"{vppPath}\"";

                    Process.Start(psi);
                    _vm?.Log("已启动 VisionPro 程序，修改后请手动保存");
                }
                else
                {
                    MessageBox.Show("未找到 VisionPro QuickBuild.exe，请确认安装路径。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _vm?.Log("启动 VisionPro 失败: " + ex.Message);
            }
        }
    }
}
