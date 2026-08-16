using System.Windows;
using VisionFramework.Core.Configuration;

namespace VisionFramework.UI.Views
{
    public partial class CameraConfigWindow : Window
    {
        public CameraConfig Config { get; private set; }

        public CameraConfigWindow(CameraConfig existing = null)
        {
            InitializeComponent();
            Config = existing ?? new CameraConfig();
            LoadConfig();
        }

        private void LoadConfig()
        {
            string[] types = { "HikCamera", "DahuaCamera", "FileCamera" };
            for (int i = 0; i < types.Length; i++)
                if (Config.CameraType == types[i]) { CbxType.SelectedIndex = i; break; }

            TxtConnStr.Text = Config.ConnectionString;

            string[] triggers = { "Software", "Hardware", "Continuous" };
            for (int i = 0; i < triggers.Length; i++)
                if (Config.TriggerMode == triggers[i]) { CbxTrigger.SelectedIndex = i; break; }

            TxtExposure.Text = Config.ExposureTime.ToString();
            TxtGain.Text = Config.Gain.ToString();
            TxtWidth.Text = Config.Width.ToString();
            TxtHeight.Text = Config.Height.ToString();

            string[] formats = { "Mono8", "BayerRG8", "RGB8" };
            for (int i = 0; i < formats.Length; i++)
                if (Config.PixelFormat == formats[i]) { CbxPixelFormat.SelectedIndex = i; break; }

            ChkFlipH.IsChecked = Config.FlipHorizontal;
            ChkFlipV.IsChecked = Config.FlipVertical;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string[] types = { "HikCamera", "DahuaCamera", "FileCamera" };
            Config.CameraType = types[CbxType.SelectedIndex];
            Config.ConnectionString = TxtConnStr.Text.Trim();
            string[] triggers = { "Software", "Hardware", "Continuous" };
            Config.TriggerMode = triggers[CbxTrigger.SelectedIndex];
            double.TryParse(TxtExposure.Text, out double exp);
            Config.ExposureTime = exp > 0 ? exp : 10000;
            double.TryParse(TxtGain.Text, out double gain);
            Config.Gain = gain;
            int.TryParse(TxtWidth.Text, out int w);
            Config.Width = w > 0 ? w : 2592;
            int.TryParse(TxtHeight.Text, out int h);
            Config.Height = h > 0 ? h : 1944;
            string[] formats = { "Mono8", "BayerRG8", "RGB8" };
            Config.PixelFormat = formats[CbxPixelFormat.SelectedIndex];
            Config.FlipHorizontal = ChkFlipH.IsChecked == true;
            Config.FlipVertical = ChkFlipV.IsChecked == true;
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
