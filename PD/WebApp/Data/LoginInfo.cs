using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace WebApp.Data;
public class LoginInfo : DatabaseConnected
{
    public bool IsLogged { get; set; } = false;
    public string? AccessToken { get; set; }
    public string? Username { get; set; }
    public string? Avatar { get; set; }
    public ulong? UserId { get; set; }
    public Role? Role { get; set; }
    public bool AlreadyChecked { get; set; } = false;

    public async Task<bool> Init(string accessToken)
    {
        var getUser = await UserModel.GetFromAccessToken(accessToken);

        if (getUser is { Status: true, Result: UserModel })
        {
            var user = (UserModel)(getUser.Result);

            var getStatus = UserAccessModel.GetUserStatus(user.UserId);
            if (getStatus is { Status: true, Result: string })
            {
                string status = (string)(getStatus.Result);
                if (status.Equals("active"))
                {
                    this.AccessToken = accessToken;
                    this.Username = user.Username;
                    this.Avatar = user.Avatar;
                    this.UserId = user.UserId;
                    this.Role = user.Role;
                    this.IsLogged = true;
                    return true;
                }
            }

        }

        UserModel.RevokeAccessToken(accessToken);
        this.AccessToken = null;
        this.Username = null;
        this.Avatar = null;
        this.UserId = null;
        this.Role = null;
        this.IsLogged = false;
        return false;
    }
}
