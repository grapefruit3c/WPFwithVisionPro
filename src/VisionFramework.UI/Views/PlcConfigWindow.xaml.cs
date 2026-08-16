using System.Windows;
using VisionFramework.Core.Configuration;

namespace VisionFramework.UI.Views
{
    public partial class PlcConfigWindow : Window
    {
        public PlcConfig Config { get; private set; }

        public PlcConfigWindow(PlcConfig existing = null)
        {
            InitializeComponent();
            Config = existing ?? new PlcConfig();
            LoadConfig();
        }

        private void LoadConfig()
        {
            TxtIp.Text = Config.IpAddress;
            TxtPort.Text = Config.Port.ToString();
            TxtTrigger.Text = Config.TriggerAddress;
            TxtTriggerAck.Text = Config.TriggerAckAddress;
            TxtResult.Text = Config.ResultAddress;
            TxtResultData.Text = Config.ResultDataAddress;
            TxtHeartbeat.Text = Config.HeartbeatAddress;
            TxtHeartbeatInterval.Text = Config.HeartbeatIntervalMs.ToString();
            TxtPingTimeout.Text = Config.PingTimeoutMs.ToString();
            CbxPlcType.SelectedIndex = Config.PlcType switch
            {
                "Mitsubishi" => 1,
                "Omron" => 2,
                "Modbus" => 3,
                _ => 0
            };
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Config.IpAddress = TxtIp.Text.Trim();
            int.TryParse(TxtPort.Text, out int port);
            Config.Port = port;
            Config.TriggerAddress = TxtTrigger.Text.Trim();
            Config.TriggerAckAddress = TxtTriggerAck.Text.Trim();
            Config.ResultAddress = TxtResult.Text.Trim();
            Config.ResultDataAddress = TxtResultData.Text.Trim();
            Config.HeartbeatAddress = TxtHeartbeat.Text.Trim();
            int.TryParse(TxtHeartbeatInterval.Text, out int hb);
            Config.HeartbeatIntervalMs = hb > 0 ? hb : 1000;
            int.TryParse(TxtPingTimeout.Text, out int pt);
            Config.PingTimeoutMs = pt > 0 ? pt : 3000;
            string[] types = { "Siemens", "Mitsubishi", "Omron", "Modbus" };
            Config.PlcType = types[CbxPlcType.SelectedIndex];
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
