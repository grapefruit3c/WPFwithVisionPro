using System.Windows;
using System.Windows.Controls;
using VisionFramework.Core.Configuration;

namespace VisionFramework.UI.Views
{
    public partial class SaveImageConfigWindow : Window
    {
        public SaveImageConfig Config { get; private set; }

        public SaveImageConfigWindow(SaveImageConfig existing = null)
        {
            InitializeComponent();
            Config = existing ?? new SaveImageConfig();
            LoadConfig();
            SldDiskLimit.ValueChanged += (s, e) => TblDiskLimit.Text = $"{(int)SldDiskLimit.Value}%";
            UpdatePreview();
            ChkAddProductId.Checked += (s, e) => UpdatePreview();
            ChkAddTimestamp.Checked += (s, e) => UpdatePreview();
            ChkAddResult.Checked += (s, e) => UpdatePreview();
            CbxFormat.SelectionChanged += (s, e) => UpdatePreview();
        }

        private void LoadConfig()
        {
            string[] fmts = { "BMP", "JPG", "PNG", "IDB" };
            for (int i = 0; i < fmts.Length; i++)
                if (Config.ImageFormat == fmts[i]) { CbxFormat.SelectedIndex = i; break; }

            TxtSavePath.Text = Config.SavePath;
            ChkSaveOriginal.IsChecked = Config.SaveOriginalImage;
            ChkSaveRendered.IsChecked = Config.SaveRenderedImage;
            ChkAddProductId.IsChecked = Config.AddProductId;
            ChkAddTimestamp.IsChecked = Config.AddTimestamp;
            ChkAddResult.IsChecked = Config.AddResult;
            ChkDateFolder.IsChecked = Config.CreateDateFolder;
            ChkResultFolder.IsChecked = Config.CreateResultFolder;
            SldDiskLimit.Value = Config.MaxDiskUsagePercent;
        }

        private void UpdatePreview()
        {
            string[] fmts = { "BMP", "JPG", "PNG", "IDB" };
            string ext = fmts[CbxFormat.SelectedIndex].ToLower();
            var parts = new System.Collections.Generic.List<string>();
            if (ChkAddProductId.IsChecked == true) parts.Add("{ProductId}");
            if (ChkAddTimestamp.IsChecked == true) parts.Add("{Timestamp}");
            if (ChkAddResult.IsChecked == true) parts.Add("{Result}");
            TblPreview.Text = (parts.Count > 0 ? string.Join("_", parts) : "image") + "." + ext;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtSavePath.Text = dlg.SelectedPath;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string[] fmts = { "BMP", "JPG", "PNG", "IDB" };
            Config.ImageFormat = fmts[CbxFormat.SelectedIndex];
            Config.SavePath = TxtSavePath.Text.Trim();
            Config.SaveOriginalImage = ChkSaveOriginal.IsChecked == true;
            Config.SaveRenderedImage = ChkSaveRendered.IsChecked == true;
            Config.AddProductId = ChkAddProductId.IsChecked == true;
            Config.AddTimestamp = ChkAddTimestamp.IsChecked == true;
            Config.AddResult = ChkAddResult.IsChecked == true;
            Config.CreateDateFolder = ChkDateFolder.IsChecked == true;
            Config.CreateResultFolder = ChkResultFolder.IsChecked == true;
            Config.MaxDiskUsagePercent = (int)SldDiskLimit.Value;
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
