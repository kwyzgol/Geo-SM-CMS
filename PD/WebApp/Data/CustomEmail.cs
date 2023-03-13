namespace WebApp.Data;

public class CustomEmail : Email
{
    public string CustomBody
    {
        get => base.Body;
        set => base.Body = value;
    }

    public string CustomSubject
    {
        get => base.Subject;
        set => base.Subject = value;
    }
}
