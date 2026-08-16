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
        }
    }
}
