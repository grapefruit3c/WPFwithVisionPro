using System.Windows;

namespace VisionFramework.UI.Views
{
    public partial class SimpleInputDialog : Window
    {
        public string Input1 { get; private set; }
        public string Input2 { get; private set; }

        public SimpleInputDialog(string title, string prompt1, string prompt2)
        {
            InitializeComponent();
            Title = title;
            LblPrompt1.Text = prompt1;
            LblPrompt2.Text = prompt2;
            if (string.IsNullOrEmpty(prompt2))
            {
                LblPrompt2.Visibility = Visibility.Collapsed;
            }
            TxtInput1.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Input1 = TxtInput1.Text.Trim();
            Input2 = TxtInput2.Text.Trim();
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
