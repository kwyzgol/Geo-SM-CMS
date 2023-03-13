namespace WebApp.Data;

public class UserModel : DatabaseConnected
{
    public string? Username { get; set; }
    public string? Avatar { get; set; }
    public ulong? UserId { get; set; }
    public Role? Role { get; set; }
    public int? Reputation { get; set; }

    public UserModel()
    {
        Username = null;
        Avatar = null;
        UserId = null;
        Role = null;
        Reputation = null;
    }

    public static async Task<OperationResult> Get(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ?
            await databases.GetUser((ulong)id) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Get(string? username, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return username != null ?
            await databases.GetUser(username) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> GetFromAccessToken(string? accessToken,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null
            ? await databases.GetUserFromAccessToken(accessToken)
            : new OperationResult(false, "Error");
    }

    public static OperationResult Login(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ?
            databases.Login((ulong)id) :
            new OperationResult(false, "Error");
    }

    public static OperationResult RevokeAccessToken(string? accessToken, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null ?
            databases.RevokeAccessToken(accessToken) :
            new OperationResult(false, "Error");
    }

    public static OperationResult RevokeAllAccessTokens(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ?
            databases.RevokeAllAccessTokens((ulong)id) :
            new OperationResult(false, "Error");
    }

    public static OperationResult GetId(string? username, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return username != null ?
            databases.GetUserId(username) :
            new OperationResult(false, "Error");
    }

    public static OperationResult GetStatusFromAccessToken(string accessToken, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return databases.GetUserStatusFromAccessToken(accessToken);
    }

    public static async Task<OperationResult> GetStatusFromAccessToken(string accessToken, Role role,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return await databases.GetUserStatusFromAccessToken(accessToken, role);
    }

    public static bool IsActive(string? accessToken)
    {
        if (accessToken == null) return false;

        var getStatus = GetStatusFromAccessToken(accessToken);

        if (getStatus is { Status: true, Result: string })
        {
            string status = (string)(getStatus.Result);
            if (status.Equals("active"))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<bool> IsActive(string? accessToken, Role role)
    {
        if (accessToken == null) return false;

        var getStatus = await GetStatusFromAccessToken(accessToken, role);

        if (getStatus is { Status: true, Result: string })
        {
            string status = (string)(getStatus.Result);
            if (status.Equals("active"))
            {
                return true;
            }
        }

        return false;
    }

    public static async Task<OperationResult> UpdateAvatar(string? accessToken, string? filename,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && filename != null
            ? await databases.UpdateAvatar(accessToken, filename)
            : new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> ChangeRole(string? accessToken, ulong? userId, Role? role,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && userId != null && role != null
            ? await databases.ChangeRole(accessToken, (ulong)userId, (Role)role)
            : new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Delete(string? accessToken, ulong? userIdToDelete = null,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null
            ? await databases.DeleteUser(accessToken, userIdToDelete)
            : new OperationResult(false, "Error");
    }

    public OperationResult GetLoginHistory(string? accessToken)
    {
        return accessToken != null && UserId != null
            ? Databases.GetLoginHistory((ulong)UserId, accessToken)
            : new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Ban(string? accessToken, string? username, string? reason, uint? days,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && username != null && reason != null && days != null ?
            await databases.BanUser(accessToken, username, reason, (uint)days) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> UnBan(string? accessToken, string? username,
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && username != null ?
            await databases.UnBanUser(accessToken, username) :
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> GetBanHistory()
    {
        return await Databases.GetBanHistory(this);
    }
}

