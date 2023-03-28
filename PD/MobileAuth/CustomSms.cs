namespace MobileAuth;

public partial class CustomSms
{
    public string Recipient { get; set; } = "";
    public string Content { get; set; } = "";

    public partial void Send();
}

