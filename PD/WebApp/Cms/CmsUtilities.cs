namespace WebApp.Cms;
public static class CmsUtilities
{
    public static Role RoleToEnum(string role)
    {
        switch (role)
        {
            case "admin":
                return Role.Admin;

            case "moderator":
                return Role.Moderator;

            case "fact checker":
                return Role.FactChecker;

            case "user":
                return Role.User;

            default:
                return Role.User;
        }
    }

    public static string RoleToString(Role role)
    {
        switch (role)
        {
            case Role.Admin:
                return "admin";

            case Role.Moderator:
                return "moderator";

            case Role.FactChecker:
                return "fact checker";

            case Role.User:
                return "user";

            default:
                return "";
        }
    }

    public static ReportType ReportTypeToEnum(string reportType)
    {
        switch (reportType)
        {
            case "moderator":
                return ReportType.Moderator;

            case "fact checker":
                return ReportType.FactChecker;

            default:
                return ReportType.Moderator;
        }
    }

    public static string ReportTypeToString(ReportType reportType)
    {
        switch (reportType)
        {
            case ReportType.Moderator:
                return "moderator";

            case ReportType.FactChecker:
                return "fact checker";

            default:
                return "";
        }
    }

    public static AuthType AuthTypeToEnum(string authType)
    {
        switch (authType)
        {
            case "none":
                return AuthType.None;

            case "email":
                return AuthType.Email;

            case "sms":
                return AuthType.Sms;

            case "email and sms":
                return AuthType.EmailAndSms;

            default:
                return AuthType.None;
        }
    }

    public static string AuthTypeToString(AuthType authType)
    {
        switch (authType)
        {
            case AuthType.None:
                return "none";

            case AuthType.Email:
                return "email";

            case AuthType.Sms:
                return "sms";

            case AuthType.EmailAndSms:
                return "email and sms";

            default:
                return "none";
        }
    }

    public static string RoleToString(AuthType authType)
    {
        switch (authType)
        {
            case AuthType.None:
                return "none";

            case AuthType.Email:
                return "email";

            case AuthType.Sms:
                return "sms";

            case AuthType.EmailAndSms:
                return "email and sms";

            default:
                return "none";
        }
    }

    public static int GetRandomAuthCode()
    {
        Random random = new Random();
        return random.Next(900000) + 100000;
    }

    public static string ViewModeToString(ViewMode viewMode)
    {
        switch (viewMode)
        {
            case ViewMode.New:
                return "new";

            case ViewMode.Best24:
                return "best24";

            default:
                return "best24";
        }
    }

    public static ViewMode ViewModeToEnum(string viewMode)
    {
        switch (viewMode)
        {
            case "new":
                return ViewMode.New;

            case "best24":
                return ViewMode.Best24;

            default:
                return ViewMode.Best24;
        }
    }
}
