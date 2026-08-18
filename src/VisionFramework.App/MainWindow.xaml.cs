using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VisionFramework.App.ViewModels;
using VisionFramework.Core.Configuration;
using VisionFramework.Core.Devices;
using VisionFramework.Devices.Plc;
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
        private ProgramConfig _programConfig = new ProgramConfig();
        private bool _isLoggedIn = false;

        // PLC 通信
        private HslPlcCommunicator _plc;
        private System.Windows.Threading.DispatcherTimer _plcPollTimer;
        private System.Windows.Threading.DispatcherTimer _heartbeatTimer;
        private bool _lastTriggerState;
        private bool _heartbeatToggle;
        private short _lastProgramNumber = -1;
        private DateTime _lastPingTime = DateTime.Now;

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
            LightTrigger.SetState(LightState.Off);
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

        // ═══ PLC 连接管理 ═══
        private void ConnectPlc()
        {
            try
            {
                DisconnectPlc();

                _plc = new HslPlcCommunicator();
                _plc.SetPlcType(_plcConfig.PlcType);
                _plc.ErrorOccurred += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _vm?.Log($"PLC 错误: {e.Message}");
                        LightPlc.SetState(LightState.Red);
                    });
                };
                _plc.Disconnected += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _vm?.Log("PLC 连接已断开");
                        LightPlc.SetState(LightState.Red);
                        LightHeartbeat.SetState(LightState.Off);
                        LightPing.SetState(LightState.Off);
                    });
                };

                bool ok;
                if (_plcConfig.PlcType == "Siemens")
                    ok = _plc.Connect(_plcConfig.IpAddress, _plcConfig.Port, _plcConfig.Rack, _plcConfig.Slot);
                else
                    ok = _plc.Connect(_plcConfig.IpAddress, _plcConfig.Port);

                if (ok)
                {
                    _vm?.Log($"PLC 已连接: {_plcConfig.IpAddress}:{_plcConfig.Port}");
                    LightPlc.SetState(LightState.Green);
                    StartPlcPolling();
                    StartHeartbeat();
                }
                else
                {
                    _vm?.Log("PLC 连接失败");
                    LightPlc.SetState(LightState.Red);
                    _plc = null;
                }
            }
            catch (Exception ex)
            {
                _vm?.Log("PLC 连接异常: " + ex.Message);
                LightPlc.SetState(LightState.Red);
            }
        }

        private void DisconnectPlc()
        {
            StopPlcPolling();
            StopHeartbeat();
            _plc?.Disconnect();
            _plc?.Dispose();
            _plc = null;
            LightPlc.SetState(LightState.Red);
            LightHeartbeat.SetState(LightState.Off);
            LightPing.SetState(LightState.Off);
        }

        // ═══ PLC 轮询（触发信号 + 程序号 + Ping） ═══
        private void StartPlcPolling()
        {
            _plcPollTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _plcPollTimer.Tick += PlcPollTimer_Tick;
            _plcPollTimer.Start();
        }

        private void StopPlcPolling()
        {
            _plcPollTimer?.Stop();
            _plcPollTimer = null;
        }

        private void PlcPollTimer_Tick(object sender, EventArgs e)
        {
            if (_plc == null || !_plc.IsConnected) return;

            try
            {
                // Ping 检测
                _lastPingTime = DateTime.Now;
                LightPing.SetState(LightState.Green);

                // 读取触发信号（上升沿触发）
                bool trigger = _plc.ReadBool(_plcConfig.TriggerAddress);
                if (trigger && !_lastTriggerState)
                {
                    // PLC 写入触发信号 → 应答 → 运行视觉 → 回写结果
                    _plc.Write(_plcConfig.TriggerAckAddress, true);
                    _vm?.Log("收到 PLC 触发信号，开始检测...");
                    _lastTriggerState = true;

                    // 相机触发指示灯闪烁
                    LightTrigger.SetState(LightState.Yellow, blink: true);

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            _vm.RunCommand.Execute(null);
                            Dispatcher.Invoke(() =>
                            {
                                bool isOk = _vm.StatusText == "OK";
                                _plc.Write(_plcConfig.ResultAddress, isOk);
                                _plc.Write(_plcConfig.TriggerAckAddress, false);
                                LightTrigger.SetState(LightState.Off);
                                _vm?.Log($"检测完成，结果已回写 PLC: {(isOk ? "OK" : "NG")}");
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _plc.Write(_plcConfig.ResultAddress, false);
                                _plc.Write(_plcConfig.TriggerAckAddress, false);
                                LightTrigger.SetState(LightState.Off);
                                _vm?.Log("检测异常: " + ex.Message);
                            });
                        }
                    });
                }
                else if (!trigger && _lastTriggerState)
                {
                    _lastTriggerState = false;
                }

                // 读取程序号（变化时切换 VPP）
                try
                {
                    short progNum = _plc.ReadShort(_plcConfig.ProgramNumberAddress);
                    if (progNum != _lastProgramNumber)
                    {
                        _lastProgramNumber = progNum;
                        int num = progNum <= 0 ? 1 : progNum;
                        _vm?.Log($"PLC 程序号变更: {progNum} → 切换到程序 {num}");
                        SelectProgram(num);
                    }
                }
                catch { }
            }
            catch
            {
                LightPing.SetState(LightState.Red);
                if ((DateTime.Now - _lastPingTime).TotalMilliseconds > _plcConfig.PingTimeoutMs)
                {
                    _vm?.Log("PLC 通讯超时，尝试重连...");
                    LightPlc.SetState(LightState.Red);
                }
            }
        }

        // ═══ 心跳 ═══
        private void StartHeartbeat()
        {
            // 指示灯闪烁（绿→灭交替），直观显示心跳在运行
            LightHeartbeat.SetState(LightState.Green, blink: true);

            _heartbeatTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_plcConfig.HeartbeatIntervalMs)
            };
            _heartbeatTimer.Tick += (s, e) =>
            {
                if (_plc == null || !_plc.IsConnected) return;
                try
                {
                    _heartbeatToggle = !_heartbeatToggle;
                    _plc.Write(_plcConfig.HeartbeatAddress, _heartbeatToggle);
                }
                catch { }
            };
            _heartbeatTimer.Start();
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer?.Stop();
            _heartbeatTimer = null;
        }

        // ═══ 配置弹窗（需登录） ═══
        private void BtnPlcConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new PlcConfigWindow(_plcConfig);
            if (dlg.ShowDialog() == true)
            {
                _plcConfig = dlg.Config;
                _vm?.Log($"PLC 配置已更新: {_plcConfig.IpAddress}:{_plcConfig.Port} | Rack={_plcConfig.Rack} Slot={_plcConfig.Slot}");
                // 配置更新后自动重连
                ConnectPlc();
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
            var monitor = new PlcMonitorWindow(_plc, _plcConfig) { Owner = this };
            monitor.Show();
        }

        private void BtnRecordHistory_Click(object sender, RoutedEventArgs e)
        {
            var win = new RecordHistoryWindow(_vm.RecordService) { Owner = this };
            win.Show();
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
                string exePath = SystemInfo.VisionProPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    MessageBox.Show("VisionPro 路径无效，请在系统信息中设置 QuickBuild.exe 路径。", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string vppPath = _vm?.CurrentVppPath;
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
            catch (Exception ex)
            {
                _vm?.Log("启动 VisionPro 失败: " + ex.Message);
            }
        }

        // ═══ 程序配置（VPP 路径列表） ═══
        private void BtnProgramConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var dlg = new ProgramConfigWindow(_programConfig) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _programConfig = dlg.Config;
                _vm?.Log($"程序配置已更新: {_programConfig.Programs.Count} 个程序");
            }
        }

        // ═══ PLC 程序号选择（PLC 发送 1→程序1, 2→程序2, 0/未发送→默认程序1） ═══
        public void SelectProgram(int programNumber)
        {
            string vppPath = _programConfig.GetVppPath(programNumber);
            if (!string.IsNullOrEmpty(vppPath) && File.Exists(vppPath))
            {
                _vm?.LoadVppFromPath(vppPath);
                _vm?.Log($"已选择程序 {programNumber}: {Path.GetFileName(vppPath)}");
            }
            else
            {
                _vm?.Log($"程序 {programNumber} 未配置 VPP 路径，使用当前程序");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            DisconnectPlc();
            base.OnClosed(e);
        }
    }
}
