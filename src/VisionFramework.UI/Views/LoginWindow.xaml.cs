using System.Windows;

namespace VisionFramework.UI.Views
{
    public partial class LoginWindow : Window
    {
        public bool LoginSuccess { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();
            TxtPassword.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (username == "admin" && string.IsNullOrEmpty(password))
            {
                LoginSuccess = true;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("用户名或密码错误", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
