using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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

        private System.Windows.Threading.DispatcherTimer _refreshTimer;
        private Random _sim = new Random();

        public PlcMonitorWindow()
        {
            InitializeComponent();
            DgSignals.ItemsSource = Signals;

            Signals.Add(new PlcSignalItem { Name = "触发信号", Address = "M100.0", DataType = "Bool" });
            Signals.Add(new PlcSignalItem { Name = "触发应答", Address = "M100.1", DataType = "Bool" });
            Signals.Add(new PlcSignalItem { Name = "结果信号", Address = "M200.0", DataType = "Bool" });
            Signals.Add(new PlcSignalItem { Name = "结果数据", Address = "D100", DataType = "Int16" });
            Signals.Add(new PlcSignalItem { Name = "心跳信号", Address = "M300.0", DataType = "Bool" });

            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _refreshTimer.Tick += (s, e) => RefreshValues();
            ChkAutoRefresh.Checked += (s, e) => _refreshTimer.Start();
            ChkAutoRefresh.Unchecked += (s, e) => _refreshTimer.Stop();
        }

        private void RefreshValues()
        {
            foreach (var item in Signals)
            {
                item.Value = item.DataType == "Bool"
                    ? (_sim.Next(2) == 1 ? "TRUE" : "FALSE")
                    : _sim.Next(0, 9999).ToString();
                item.LastUpdate = DateTime.Now.ToString("HH:mm:ss.fff");
            }
            DgSignals.Items.Refresh();
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
