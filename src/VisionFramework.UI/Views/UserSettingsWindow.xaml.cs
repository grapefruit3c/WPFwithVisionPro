using System.Windows;

namespace VisionFramework.UI.Views
{
    public partial class UserSettingsWindow : Window
    {
        private string _username = "admin";
        private string _password = "";

        public UserSettingsWindow(string username = "admin")
        {
            InitializeComponent();
            _username = username;
            TblUsername.Text = username;
        }

        private void BtnChange_Click(object sender, RoutedEventArgs e)
        {
            string oldPwd = TxtOldPassword.Password;
            string newPwd = TxtNewPassword.Password;
            string confirmPwd = TxtConfirmPassword.Password;

            if (oldPwd != _password)
            {
                MessageBox.Show("当前密码错误", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(newPwd))
            {
                MessageBox.Show("新密码不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPwd != confirmPwd)
            {
                MessageBox.Show("两次输入的密码不一致", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _password = newPwd;
            MessageBox.Show("密码修改成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtOldPassword.Clear();
            TxtNewPassword.Clear();
            TxtConfirmPassword.Clear();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
