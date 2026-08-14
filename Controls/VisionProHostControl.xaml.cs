using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.QuickBuild;
using VisionProVppHost.Core;
using WinForms = System.Windows.Forms;

namespace VisionProVppHost.Controls
{
    /// <summary>
    /// 通用 VisionPro VPP 宿主控件：
    /// 加载 .vpp（CogToolBlock / CogJobManager / 单工具）→ 动态枚举输入输出终端 → 运行 → 显示结果记录。
    /// 可作为 UserControl 复用到任意 WPF 窗体中。
    /// </summary>
    public partial class VisionProHostControl : UserControl
    {
        private CogDisplay _imgDisplay;       // 显示原始图像（CogDisplay.Image）
        private CogRecordDisplay _recDisplay; // 显示运行结果记录（CogRecordDisplay.Record）
        private object _vpp;
        private VppKind _kind;
        private ICogImage _currentImage;
        private readonly Dictionary<string, TextBox> _inputBoxes = new Dictionary<string, TextBox>();

        public VisionProHostControl()
        {
            InitializeComponent();
            _imgDisplay = new CogDisplay { Dock = WinForms.DockStyle.Fill };
            _recDisplay = new CogRecordDisplay { Dock = WinForms.DockStyle.Fill };
            ImageHost.Child = _imgDisplay;
            RecordHost.Child = _recDisplay;
        }

        // ---------------- 加载 VPP ----------------
        private void BtnLoadVpp_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "VisionPro VPP|*.vpp|所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            try { LoadVpp(dlg.FileName); }
            catch (Exception ex) { Log("加载失败: " + ex.Message); }
        }

        private void LoadVpp(string path)
        {
            _vpp = CogSerializer.LoadObjectFromFile(path);
            _inputBoxes.Clear();
            OutputsPanel.Children.Clear();
            _currentImage = null;

            if (_vpp is CogToolBlock tb)
            {
                _kind = VppKind.ToolBlock;
                TbVppInfo.Text = "类型: CogToolBlock\n路径: " + path;
                BuildToolBlockInputs(tb);
                BtnRun.IsEnabled = true;
                BtnFit.IsEnabled = true;
                Log("已加载工具块: " + Path.GetFileName(path));
            }
            else if (_vpp is CogJobManager jm)
            {
                _kind = VppKind.JobManager;
                BuildJobInputs(jm, out int n);
                TbVppInfo.Text = "类型: CogJobManager(QuickBuild)\n路径: " + path + "\nJob 数: " + n;
                // QuickBuild 模式始终允许加载图片——运行时会绕过 AcqFifo 直接喂图给视觉工具
                BtnLoadImage.IsEnabled = true;
                BtnRun.IsEnabled = true;
                BtnFit.IsEnabled = true;
                Log("已加载 QuickBuild: " + Path.GetFileName(path) + "，Job 数 " + n);
                // 打印每个 Job 的视觉工具类型，方便诊断
                for (int i = 0; i < jm.JobCount; i++)
                {
                    CogJob job = jm.Job(i);
                    string vtName = job?.VisionTool?.GetType().Name ?? "null";
                    Log("  Job " + i + " 视觉工具类型: " + vtName);
                }
            }
            else if (_vpp is ICogTool tool)
            {
                _kind = VppKind.Tool;
                TbVppInfo.Text = "类型: " + _vpp.GetType().Name + "\n路径: " + path;
                InputsPanel.Children.Clear();
                InputsPanel.Children.Add(MakeNote("单一工具，无可编辑终端，直接点击运行。"));
                BtnLoadImage.IsEnabled = false;
                BtnRun.IsEnabled = true;
                BtnFit.IsEnabled = true;
                Log("已加载工具: " + _vpp.GetType().Name);
            }
            else
            {
                _kind = VppKind.Unknown;
                TbVppInfo.Text = "不支持的类型: " + (_vpp?.GetType().Name ?? "null");
                BtnLoadImage.IsEnabled = BtnRun.IsEnabled = BtnFit.IsEnabled = false;
                Log("不支持的 VPP 类型");
            }
        }

        // ---------------- 输入终端 UI ----------------
        private void BuildToolBlockInputs(CogToolBlock tb)
        {
            InputsPanel.Children.Clear();
            bool anyImage = false;
            foreach (CogToolBlockTerminal t in tb.Inputs)
            {
                bool isImage = typeof(ICogImage).IsAssignableFrom(t.ValueType);
                if (isImage) anyImage = true;

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
                row.Children.Add(new TextBlock
                {
                    Text = t.Name + " (" + (t.ValueType?.Name ?? "?") + ")",
                    Width = 160,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                });

                if (isImage)
                {
                    row.Children.Add(new TextBlock
                    {
                        Text = "[图像终端]",
                        Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }
                else
                {
                    var box = new TextBox { Width = 130, Tag = t.Name };
                    box.Text = t.Value == null ? "" : t.Value.ToString();
                    row.Children.Add(box);
                    _inputBoxes[t.Name] = box;
                }
                InputsPanel.Children.Add(row);
            }
            if (InputsPanel.Children.Count == 0)
                InputsPanel.Children.Add(MakeNote("该工具块无输入终端。"));

            BtnLoadImage.IsEnabled = anyImage;
        }

        private void BuildJobInputs(CogJobManager jm, out int count)
        {
            InputsPanel.Children.Clear();
            int i;
            for (i = 0; i < jm.JobCount; i++)
            {
                CogJob job = jm.Job(i);
                var rb = new RadioButton
                {
                    Content = "Job " + i + ": " + (job?.Name ?? i.ToString()),
                    Tag = i,
                    IsChecked = (i == 0),
                    Margin = new Thickness(0, 0, 0, 4)
                };
                InputsPanel.Children.Add(rb);
            }
            count = i;
            if (i == 0) InputsPanel.Children.Add(MakeNote("未发现 Job。"));
        }

        private UIElement MakeNote(string text) =>
            new TextBlock { Text = text, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap };

        // ---------------- 加载图片 ----------------
        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "VisionPro 图像|*.idb;*.cdb|位图|*.bmp;*.jpg;*.jpeg;*.png;*.tif;*.tiff|所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _currentImage = CogImageHelper.LoadAsCogImage(dlg.FileName);
                TbImageInfo.Text = "图像: " + Path.GetFileName(dlg.FileName) + "  " + _currentImage.Width + "x" + _currentImage.Height;

                if (_kind == VppKind.ToolBlock && _vpp is CogToolBlock tb)
                {
                    foreach (CogToolBlockTerminal t in tb.Inputs)
                        if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                            t.Value = _currentImage;
                }
                // JobManager 模式不需要在这里设值，运行时再处理

                ShowImage(_currentImage);
                Log("已加载图像: " + Path.GetFileName(dlg.FileName));
            }
            catch (Exception ex) { Log("图像加载失败: " + ex.Message); }
        }

        // ---------------- 运行 ----------------
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplySimpleInputs();
                ICogRecord record = null;

                if (_kind == VppKind.ToolBlock && _vpp is CogToolBlock tb)
                {
                    tb.Run();
                    record = tb.CreateLastRunRecord();
                }
                else if (_kind == VppKind.JobManager && _vpp is CogJobManager jm)
                {
                    int idx = GetSelectedJobIndex();
                    CogJob job = jm.Job(idx);

                    if (_currentImage != null)
                    {
                        // 有手动加载的图片：绕过 AcqFifo，直接喂图给视觉工具
                        record = RunJobWithImage(job, _currentImage);
                        Log("使用手动图片运行 Job " + idx);
                    }
                    else
                    {
                        // 没有手动图片：走 Job 正常流程（使用 AcqFifo）
                        job.Run();
                        record = job.OwnedIndependent.RealTimeResult();
                        Log("使用内部图像源运行 Job " + idx);
                    }
                }
                else if (_kind == VppKind.Tool && _vpp is ICogTool tool)
                {
                    tool.Run();
                    record = tool.CreateLastRunRecord();
                }

                if (record != null) ShowRecord(record);
                RefreshOutputs();
                Log("运行完成");
            }
            catch (Exception ex) { Log("运行失败: " + ex.Message); }
        }

        /// <summary>
        /// 绕过 AcqFifo，直接把图片塞给 Job 的视觉工具并运行。
        /// 支持 CogToolBlock（终端）、CogToolGroup（遍历子工具 + 脚本终端）和其他工具（反射）。
        /// </summary>
        private ICogRecord RunJobWithImage(CogJob job, ICogImage img)
        {
            ICogTool vt = job.VisionTool;
            if (vt == null)
                throw new InvalidOperationException("该 Job 没有视觉工具（VisionTool 为 null）");

            Log("视觉工具类型: " + vt.GetType().Name);

            // 策略1：CogToolBlock — 设置图像输入终端
            if (vt is CogToolBlock tb)
            {
                SetImageOnToolBlock(tb, img);
            }
            // 策略2：CogToolGroup — 遍历子工具设置图片，并尝试脚本终端
            else if (vt is CogToolGroup tg)
            {
                SetImageOnToolGroup(tg, img);
            }
            // 策略3：其他工具 — 反射设置 InputImage
            else
            {
                TrySetInputImage(vt, img);
            }

            // 运行视觉工具（不是 Job.Run，避免触发 AcqFifo）
            vt.Run();

            // 获取运行记录
            ICogRecord record = vt.CreateLastRunRecord();
            if (record == null)
            {
                record = job.OwnedIndependent.RealTimeResult();
            }
            return record;
        }

        /// <summary>
        /// 在 CogToolBlock 上设置图像输入终端。
        /// </summary>
        private void SetImageOnToolBlock(CogToolBlock tb, ICogImage img)
        {
            bool setOk = false;
            foreach (CogToolBlockTerminal t in tb.Inputs)
            {
                if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                {
                    t.Value = img;
                    setOk = true;
                }
            }
            if (setOk)
                Log("已通过 CogToolBlock 输入终端设置图像");
            else
            {
                // 无图像终端，尝试反射
                TrySetInputImage(tb, img);
            }
        }

        /// <summary>
        /// 在 CogToolGroup 上设置图像：
        /// 1. 尝试 SetScriptTerminalData（常见终端名）
        /// 2. 遍历 Tools 集合，对每个子工具尝试设置图像
        /// </summary>
        private void SetImageOnToolGroup(CogToolGroup tg, ICogImage img)
        {
            bool setOk = false;

            // 方法1：尝试通过脚本终端设置图像（常见终端名）
            string[] commonKeys = { "InputImage", "Image", "inputImage", "image", "InputImageTerminal" };
            foreach (string key in commonKeys)
            {
                try
                {
                    // SetScriptTerminalData 返回 bool 表示是否成功
                    var method = tg.GetType().GetMethod("SetScriptTerminalData",
                        new[] { typeof(string), typeof(object) });
                    if (method != null)
                    {
                        bool result = (bool)method.Invoke(tg, new object[] { key, img });
                        if (result)
                        {
                            Log("已通过脚本终端 '" + key + "' 设置图像");
                            setOk = true;
                            break;
                        }
                    }
                }
                catch { }
            }

            // 方法2：遍历子工具，尝试设置图像
            try
            {
                var tools = tg.Tools;
                if (tools != null)
                {
                    foreach (var subTool in tools)
                    {
                        if (subTool is CogToolBlock subTb)
                        {
                            foreach (CogToolBlockTerminal t in subTb.Inputs)
                            {
                                if (typeof(ICogImage).IsAssignableFrom(t.ValueType))
                                {
                                    t.Value = img;
                                    setOk = true;
                                    Log("已通过子工具 " + subTb.Name + " 的图像终端设置图像");
                                }
                            }
                        }
                        else
                        {
                            // 非ToolBlock子工具，尝试反射设置 InputImage
                            if (TrySetInputImage(subTool, img))
                                setOk = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("遍历子工具时异常: " + ex.Message);
            }

            if (!setOk)
                Log("警告: CogToolGroup 未能设置图像，将使用内部已有图像运行");
        }

        /// <summary>
        /// 通过反射设置工具的 InputImage 属性（兼容非 CogToolBlock 工具）。
        /// 返回 true 表示设置成功。
        /// </summary>
        private bool TrySetInputImage(object tool, ICogImage img)
        {
            try
            {
                var prop = tool.GetType().GetProperty("InputImage");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(tool, img);
                    Log("已通过反射设置 " + tool.GetType().Name + ".InputImage");
                    return true;
                }
            }
            catch { }
            return false;
        }

        private void ApplySimpleInputs()
        {
            if (!(_kind == VppKind.ToolBlock && _vpp is CogToolBlock tb)) return;
            foreach (var kv in _inputBoxes)
            {
                if (!tb.Inputs.Contains(kv.Key)) continue;
                var terminal = tb.Inputs[kv.Key];
                try { terminal.Value = ParseValue(kv.Value.Text, terminal.ValueType); }
                catch (Exception ex) { Log("输入 " + kv.Key + " 解析失败: " + ex.Message); }
            }
        }

        private int GetSelectedJobIndex()
        {
            foreach (var child in InputsPanel.Children)
                if (child is RadioButton rb && rb.IsChecked == true && rb.Tag is int i)
                    return i;
            return 0;
        }

        private void RefreshOutputs()
        {
            OutputsPanel.Children.Clear();
            if (_kind == VppKind.ToolBlock && _vpp is CogToolBlock tb)
            {
                foreach (CogToolBlockTerminal t in tb.Outputs)
                {
                    var row = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
                    row.Children.Add(new TextBlock { Text = t.Name, FontWeight = FontWeights.Bold });
                    row.Children.Add(new TextBlock { Text = FormatValue(t.Value), TextWrapping = TextWrapping.Wrap });
                    OutputsPanel.Children.Add(row);
                }
                if (OutputsPanel.Children.Count == 0)
                    OutputsPanel.Children.Add(MakeNote("无输出终端"));
            }
            else if (_kind == VppKind.JobManager && _vpp is CogJobManager jm)
            {
                // 尝试枚举选中 Job 的视觉工具输出终端
                int idx = GetSelectedJobIndex();
                CogJob job = jm.Job(idx);
                if (job?.VisionTool is CogToolBlock jobTb)
                {
                    foreach (CogToolBlockTerminal t in jobTb.Outputs)
                    {
                        var row = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
                        row.Children.Add(new TextBlock { Text = t.Name, FontWeight = FontWeights.Bold });
                        row.Children.Add(new TextBlock { Text = FormatValue(t.Value), TextWrapping = TextWrapping.Wrap });
                        OutputsPanel.Children.Add(row);
                    }
                    if (OutputsPanel.Children.Count == 0)
                        OutputsPanel.Children.Add(MakeNote("无输出终端"));
                }
                else
                {
                    OutputsPanel.Children.Add(MakeNote("该 Job 视觉工具非 CogToolBlock，请查看显示区记录。"));
                }
            }
            else
            {
                OutputsPanel.Children.Add(MakeNote("该类型不枚举输出终端，请查看显示区记录。"));
            }
        }

        private static string FormatValue(object v)
        {
            if (v == null) return "null";
            if (v is ICogImage img) return "[图像] " + img.Width + "x" + img.Height;
            return v.ToString();
        }

        // ---------------- 显示 ----------------
        private void ShowImage(ICogImage img)
        {
            _imgDisplay.Image = img;
            SafeFit(_imgDisplay);
            ImageHost.Visibility = Visibility.Visible;
            RecordHost.Visibility = Visibility.Collapsed;
        }

        private void ShowRecord(ICogRecord record)
        {
            // 把当前图像也设到 RecordDisplay 上，这样即使记录本身不含图像，
            // 显示区也有图像背景 + 叠加的图形结果
            if (_currentImage != null)
            {
                try { _recDisplay.Image = _currentImage; }
                catch { }
            }
            _recDisplay.Record = record;
            SafeFit(_recDisplay);
            RecordHost.Visibility = Visibility.Visible;
            ImageHost.Visibility = Visibility.Collapsed;
        }

        private void BtnFit_Click(object sender, RoutedEventArgs e)
        {
            if (RecordHost.Visibility == Visibility.Visible) SafeFit(_recDisplay);
            else SafeFit(_imgDisplay);
        }

        // Fit 签名随版本不同（Fit() 或 Fit(bool)），用反射兼容
        private static void SafeFit(object display)
        {
            if (display == null) return;
            try
            {
                var t = display.GetType();
                var fit1 = t.GetMethod("Fit", new[] { typeof(bool) });
                if (fit1 != null) { fit1.Invoke(display, new object[] { true }); return; }
                var fit0 = t.GetMethod("Fit", Type.EmptyTypes);
                fit0?.Invoke(display, null);
            }
            catch { /* 忽略 */ }
        }

        // ---------------- 日志 ----------------
        private void Log(string msg)
        {
            LogPanel.Children.Add(new TextBlock
            {
                Text = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg,
                TextWrapping = TextWrapping.Wrap
            });
            while (LogPanel.Children.Count > 200) LogPanel.Children.RemoveAt(0);
        }

        private static object ParseValue(string text, Type type)
        {
            if (type == typeof(double)) return double.Parse(text);
            if (type == typeof(float)) return float.Parse(text);
            if (type == typeof(int)) return int.Parse(text);
            if (type == typeof(long)) return long.Parse(text);
            if (type == typeof(bool)) return bool.Parse(text);
            if (type == typeof(string)) return text;
            return Convert.ChangeType(text, type);
        }

        private enum VppKind { Unknown, ToolBlock, JobManager, Tool }
    }
}
