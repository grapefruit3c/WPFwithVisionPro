using System.IO;
using System.Windows;
using VisionFramework.Core.Configuration;

namespace VisionFramework.UI.Views
{
    public partial class SystemInfoWindow : Window
    {
        public SystemInfoWindow()
        {
            InitializeComponent();
            TblVersion.Text = SystemInfo.Version;
            TblDesigner.Text = SystemInfo.Designer;
            TblBuildDate.Text = SystemInfo.BuildDate;
            TblRuntime.Text = $".NET Framework {System.Environment.Version}";
            TxtVisionProPath.Text = SystemInfo.VisionProPath;
        }

        private void BtnBrowseVp_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 QuickBuild.exe",
                Filter = "QuickBuild.exe|Cognex.VisionPro.QuickBuild.exe|可执行文件|*.exe|所有文件|*.*",
                InitialDirectory = Path.GetDirectoryName(TxtVisionProPath.Text)
            };
            if (dlg.ShowDialog() == true)
            {
                TxtVisionProPath.Text = dlg.FileName;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SystemInfo.VisionProPath = TxtVisionProPath.Text.Trim();
            base.OnClosing(e);
        }
    }
}
