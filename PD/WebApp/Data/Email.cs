using System.Net;
using System.Net.Mail;
using System.Text;

namespace WebApp.Data;

public abstract class Email
{
    protected string Subject = "";
    protected string Body = "";

    public static SmtpAccessData SmtpData { get; set; } = new SmtpAccessData();
    public string Recipient { get; set; } = "";

    public bool Send()
    {
        try
        {
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = SmtpData.Host;
            smtpClient.Port = SmtpData.Port;
            smtpClient.Credentials = new NetworkCredential(SmtpData.User, SmtpData.Password);
            string sender = Cms.Cms.System?.SystemName ?? "PD CMS";
            MailAddress fromAddress = new MailAddress(SmtpData.User, sender);
            MailAddress toAddress = new MailAddress(Recipient);
            MailMessage message = new MailMessage(fromAddress, toAddress);
            message.Subject = Subject;
            message.SubjectEncoding = Encoding.UTF8;
            message.Body = Body;
            message.BodyEncoding = Encoding.UTF8;
            message.IsBodyHtml = true;
            smtpClient.Send(message);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }
}

