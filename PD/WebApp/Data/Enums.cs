namespace WebApp.Data;

public enum Role
{
    Admin = 3,
    Moderator = 2,
    FactChecker = 1,
    User = 0
}

public enum SearchType
{
    Post = 0,
    Tag = 1,
    User = 2,
    All = 3
}

public enum Content
{
    Post = 0,
    Comment = 1,
    Message = 2
}

public enum ReportType
{
    Moderator = 0,
    FactChecker = 1
}

public enum AuthType
{
    None = 0,
    Email = 1,
    Sms = 2,
    EmailAndSms = 3,
}

public enum PostType
{
    Mini = 0,
    Full = 1
}

public enum ViewMode
{
    New = 0,
    Best24 = 1
}

public enum FormStage
{
    Form = 0,
    Processing = 1,
    Auth = 2
}

public enum AuthPanelMode
{
    Normal = 0,
    ForgotPassword = 1
}

public enum LocationType
{
    Normal = 0,
    Distance = 1
}