using System.Windows;
using VisionFramework.App.ViewModels;

namespace VisionFramework.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(DisplayControl);
        }
    }
}