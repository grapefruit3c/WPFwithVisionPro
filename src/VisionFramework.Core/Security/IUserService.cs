using System;

namespace VisionFramework.Core.Security
{
    /// <summary>
    /// 用户服务接口——登录、修改密码。
    /// </summary>
    public interface IUserService
    {
        UserAccount CurrentUser { get; }
        event EventHandler<UserAccount> UserChanged;

        bool Login(string username, string password);
        void Logout();
        bool ChangePassword(string oldPassword, string newPassword);
        bool ValidateUser(string username, string password);
    }

    /// <summary>
    /// 用户账户模型。
    /// </summary>
    public class UserAccount
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "Operator";
        public DateTime LastLogin { get; set; }

        public bool IsLoggedIn { get; set; }
    }
}
