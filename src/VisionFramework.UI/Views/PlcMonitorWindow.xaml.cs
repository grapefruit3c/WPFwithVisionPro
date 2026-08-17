using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VisionFramework.Core.Configuration;
using VisionFramework.Core.Devices;

namespace VisionFramework.UI.Views
{
    public class PlcSignalItem
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string DataType { get; set; } = "Bool";
        public string Value { get; set; } = "---";
        public string LastUpdate { get; set; } = "---";
    }

    public partial class PlcMonitorWindow : Window
    {
        public ObservableCollection<PlcSignalItem> Signals { get; } = new ObservableCollection<PlcSignalItem>();

        private readonly IPlcCommunicator _plc;
        private readonly PlcConfig _config;
        private System.Windows.Threading.DispatcherTimer _refreshTimer;

        public PlcMonitorWindow(IPlcCommunicator plc = null, PlcConfig config = null)
        {
            InitializeComponent();
            _plc = plc;
            _config = config;
            DgSignals.ItemsSource = Signals;

            // 根据配置初始化信号列表
            if (config != null)
            {
                Signals.Add(new PlcSignalItem { Name = "触发信号", Address = config.TriggerAddress, DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "触发应答", Address = config.TriggerAckAddress, DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "结果信号", Address = config.ResultAddress, DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "结果数据", Address = config.ResultDataAddress, DataType = "Int16" });
                Signals.Add(new PlcSignalItem { Name = "心跳信号", Address = config.HeartbeatAddress, DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "程序号", Address = config.ProgramNumberAddress, DataType = "Int16" });
            }
            else
            {
                Signals.Add(new PlcSignalItem { Name = "触发信号", Address = "M100.0", DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "触发应答", Address = "M100.1", DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "结果信号", Address = "M200.0", DataType = "Bool" });
                Signals.Add(new PlcSignalItem { Name = "结果数据", Address = "D100", DataType = "Int16" });
                Signals.Add(new PlcSignalItem { Name = "心跳信号", Address = "M300.0", DataType = "Bool" });
            }

            UpdateStatus();

            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += (s, e) => RefreshValues();
            ChkAutoRefresh.Checked += (s, e) => { if (_plc?.IsConnected == true) _refreshTimer.Start(); };
            ChkAutoRefresh.Unchecked += (s, e) => _refreshTimer.Stop();

            Loaded += (s, e) =>
            {
                if (ChkAutoRefresh.IsChecked == true && _plc?.IsConnected == true)
                    _refreshTimer.Start();
            };
        }

        private void UpdateStatus()
        {
            bool connected = _plc?.IsConnected == true;
            TblStatus.Text = connected ? "● 已连接" : "● 未连接";
            TblStatus.Foreground = connected
                ? System.Windows.Media.Brushes.LimeGreen
                : System.Windows.Media.Brushes.OrangeRed;
        }

        private void RefreshValues()
        {
            if (_plc == null || !_plc.IsConnected)
            {
                foreach (var item in Signals)
                {
                    item.Value = "---";
                    item.LastUpdate = DateTime.Now.ToString("HH:mm:ss.fff");
                }
                DgSignals.Items.Refresh();
                UpdateStatus();
                return;
            }

            foreach (var item in Signals)
            {
                try
                {
                    switch (item.DataType)
                    {
                        case "Bool":
                            item.Value = _plc.ReadBool(item.Address) ? "TRUE" : "FALSE";
                            break;
                        case "Int16":
                            item.Value = _plc.ReadShort(item.Address).ToString();
                            break;
                        case "Int32":
                            item.Value = _plc.ReadInt(item.Address).ToString();
                            break;
                        case "Float":
                            item.Value = _plc.ReadFloat(item.Address).ToString("F3");
                            break;
                    }
                }
                catch
                {
                    item.Value = "ERR";
                }
                item.LastUpdate = DateTime.Now.ToString("HH:mm:ss.fff");
            }
            DgSignals.Items.Refresh();
            UpdateStatus();
        }

        private void BtnAddSignal_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SimpleInputDialog("添加信号", "信号名称：", "PLC 地址：");
            if (dlg.ShowDialog() == true)
            {
                Signals.Add(new PlcSignalItem
                {
                    Name = dlg.Input1,
                    Address = dlg.Input2,
                    DataType = "Bool"
                });
            }
        }

        private void BtnRemoveSignal_Click(object sender, RoutedEventArgs e)
        {
            if (DgSignals.SelectedItem is PlcSignalItem item)
                Signals.Remove(item);
        }

        private void BtnManualRead_Click(object sender, RoutedEventArgs e)
        {
            RefreshValues();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            Close();
        }
    }
}
