using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Cognex.VisionPro;
using VisionFramework.Core.Algorithms;
using VisionFramework.UI.Controls;
using VisionFramework.UI.ViewModels;
using VisionFramework.VisionPro;

namespace VisionFramework.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly VisionDisplayControl _display;
        private IVisionAlgorithm _algorithm;
        private ICogImage _currentImage;

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();
        public ObservableCollection<TerminalInfo> InputTerminals { get; } = new ObservableCollection<TerminalInfo>();
        public ObservableCollection<TerminalInfo> OutputTerminals { get; } = new ObservableCollection<TerminalInfo>();

        private string _statusText = "就绪";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

        private string _vppInfo = "未加载 VPP";
        public string VppInfo { get => _vppInfo; set => SetProperty(ref _vppInfo, value); }

        public RelayCommand LoadVppCommand { get; }
        public RelayCommand LoadImageCommand { get; }
        public RelayCommand RunCommand { get; }
        public RelayCommand FitCommand { get; }

        public MainViewModel(VisionDisplayControl display)
        {
            _display = display;
            LoadVppCommand = new RelayCommand(LoadVpp);
            LoadImageCommand = new RelayCommand(LoadImage, () => _algorithm != null);
            RunCommand = new RelayCommand(Run, () => _algorithm != null);
            FitCommand = new RelayCommand(() => _display.Fit());
        }

        private void LoadVpp()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "VisionPro VPP|*.vpp" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _algorithm?.Dispose();
                _algorithm = AlgorithmFactory.Create(dlg.FileName);

                VppInfo = $"类型: {_algorithm.Kind}\r\n路径: {Path.GetFileName(dlg.FileName)}";
                InputTerminals.Clear();
                foreach (var t in _algorithm.GetInputTerminals())
                    InputTerminals.Add(t);
                OutputTerminals.Clear();

                Log($"已加载 {_algorithm.Name}: {Path.GetFileName(dlg.FileName)}");
                StatusText = $"{_algorithm.Name} 已就绪";
            }
            catch (Exception ex) { Log("加载失败: " + ex.Message); }
        }

        private void LoadImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "VisionPro 图像|*.idb;*.cdb|位图|*.bmp;*.jpg;*.png;*.tif|所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                _currentImage = CogImageHelper.LoadAsCogImage(dlg.FileName);
                _display.ShowImage(_currentImage);
                Log($"已加载图像: {Path.GetFileName(dlg.FileName)}");
            }
            catch (Exception ex) { Log("图像加载失败: " + ex.Message); }
        }

        private async void Run()
        {
            try
            {
                StatusText = "运行中...";
                var inputs = InputTerminals.Where(t => !t.IsImage)
                    .ToDictionary(t => t.Name, t => t.Value);

                var result = await Task.Run(() => _algorithm.Detect(_currentImage, inputs));

                OutputTerminals.Clear();
                foreach (var kv in result.Outputs)
                    OutputTerminals.Add(new TerminalInfo { Name = kv.Key, Value = FormatValue(kv.Value) });

                if (result.Record is ICogRecord rec)
                    _display.ShowRecord(_currentImage as ICogImage, rec);

                Log($"运行完成 | {(result.IsOk ? "OK" : "NG")} | {result.DurationMs}ms");
                StatusText = result.IsOk ? "OK" : "NG";
            }
            catch (Exception ex) { Log("运行失败: " + ex.Message); StatusText = "错误"; }
        }

        private void Log(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, line);
                while (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
            });
        }

        private static string FormatValue(object v)
        {
            if (v == null) return "null";
            if (v is ICogImage img) return $"[图像] {img.Width}x{img.Height}";
            return v.ToString();
        }
    }
}