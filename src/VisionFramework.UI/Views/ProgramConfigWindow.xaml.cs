using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VisionFramework.Core.Configuration;

namespace VisionFramework.UI.Views
{
    public partial class ProgramConfigWindow : Window
    {
        public ProgramConfig Config { get; private set; }
        private ObservableCollection<ProgramEntry> _entries;

        public ProgramConfigWindow(ProgramConfig existing = null)
        {
            InitializeComponent();
            Config = existing ?? new ProgramConfig();
            _entries = new ObservableCollection<ProgramEntry>(Config.Programs);
            ProgramList.ItemsSource = _entries;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            int nextNumber = _entries.Count > 0 ? _entries[_entries.Count - 1].Number + 1 : 1;
            _entries.Add(new ProgramEntry { Number = nextNumber, Name = $"程序 {nextNumber}" });
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProgramEntry entry)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "VisionPro VPP|*.vpp" };
                if (!string.IsNullOrEmpty(entry.VppPath))
                    dlg.InitialDirectory = Path.GetDirectoryName(entry.VppPath);
                if (dlg.ShowDialog() == true)
                    entry.VppPath = dlg.FileName;
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProgramEntry entry)
                _entries.Remove(entry);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Config.Programs = new List<ProgramEntry>(_entries);
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
