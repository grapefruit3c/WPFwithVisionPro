using System.Collections.ObjectModel;
using System.Windows;
using VisionFramework.Core.Data;

namespace VisionFramework.UI.Views
{
    public partial class RecordHistoryWindow : Window
    {
        private readonly DetectionRecordService _service;
        public ObservableCollection<DetectionRecord> Records { get; } = new ObservableCollection<DetectionRecord>();

        public RecordHistoryWindow(DetectionRecordService service)
        {
            InitializeComponent();
            _service = service;
            DgRecords.ItemsSource = Records;
            Refresh();
        }

        private void Refresh()
        {
            Records.Clear();
            foreach (var r in _service.GetRecent(500))
                Records.Add(r);

            var (total, ok, ng) = _service.GetStats();
            TblStats.Text = $"共 {total} 条  |  OK: {ok}  |  NG: {ng}";
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
