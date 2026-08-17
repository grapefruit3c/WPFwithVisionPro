using System.Windows;
using VisionFramework.Core.Configuration;
using VisionFramework.Devices.Plc;

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
            TxtRack.Text = Config.Rack.ToString();
            TxtSlot.Text = Config.Slot.ToString();
            TxtTrigger.Text = Config.TriggerAddress;
            TxtTriggerAck.Text = Config.TriggerAckAddress;
            TxtResult.Text = Config.ResultAddress;
            TxtResultData.Text = Config.ResultDataAddress;
            TxtHeartbeat.Text = Config.HeartbeatAddress;
            TxtProgramNumber.Text = Config.ProgramNumberAddress;
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

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            string ip = TxtIp.Text.Trim();
            int.TryParse(TxtPort.Text, out int port);
            byte.TryParse(TxtRack.Text, out byte rack);
            byte.TryParse(TxtSlot.Text, out byte slot);
            string[] types = { "Siemens", "Mitsubishi", "Omron", "Modbus" };
            string plcType = types[CbxPlcType.SelectedIndex];

            BtnTestConnection.IsEnabled = false;
            BtnTestConnection.Content = "连接中...";

            var testPlc = new HslPlcCommunicator();
            testPlc.SetPlcType(plcType);

            bool ok;
            if (plcType == "Siemens")
                ok = testPlc.Connect(ip, port, rack, slot);
            else
                ok = testPlc.Connect(ip, port);

            testPlc.Disconnect();

            BtnTestConnection.IsEnabled = true;
            BtnTestConnection.Content = "测试连接";

            MessageBox.Show(ok ? "连接成功！" : "连接失败，请检查 IP、端口和 PLC 类型。",
                "测试连接", MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Config.IpAddress = TxtIp.Text.Trim();
            int.TryParse(TxtPort.Text, out int port);
            Config.Port = port;
            byte.TryParse(TxtRack.Text, out byte rack);
            byte.TryParse(TxtSlot.Text, out byte slot);
            Config.Rack = rack;
            Config.Slot = slot;
            Config.TriggerAddress = TxtTrigger.Text.Trim();
            Config.TriggerAckAddress = TxtTriggerAck.Text.Trim();
            Config.ResultAddress = TxtResult.Text.Trim();
            Config.ResultDataAddress = TxtResultData.Text.Trim();
            Config.HeartbeatAddress = TxtHeartbeat.Text.Trim();
            Config.ProgramNumberAddress = TxtProgramNumber.Text.Trim();
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
