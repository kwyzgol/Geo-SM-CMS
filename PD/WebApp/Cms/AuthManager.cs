namespace WebApp.Cms;

public class AuthManager : DatabaseConnected
{
    private string _accessKey = Guid.NewGuid().ToString();

    public string AccessKey
    {
        get => _accessKey;
        set
        {
            _accessKey = value;
            AuthHub.AccessKey = value;
        }
    }
    public SmtpAccessData SmtpData { get; set; } = new SmtpAccessData();
    public AuthType Type { get; set; } = AuthType.None;
    public List<PhoneAllowed> PhoneAllowedList { get; set; } = new List<PhoneAllowed>();

    public void SetSmtpEmail()
    {
        Email.SmtpData = SmtpData;
    }

    public AuthManager() { }
    public AuthManager(string accessKey, SmtpAccessData smtpData, AuthType type, List<PhoneAllowed> phoneAllowedList)
    {
        AccessKey = accessKey;
        SmtpData = smtpData;
        AccessKey = accessKey;
        Type = type;
        SmtpData = smtpData;
        PhoneAllowedList = phoneAllowedList;
    }

    public bool CheckNumber(string phoneNumber)
    {
        foreach (var phoneAllowed in PhoneAllowedList)
        {
            if (phoneAllowed.Verify(phoneNumber))
            {
                return true;
            }
        }

        return false;
    }
}