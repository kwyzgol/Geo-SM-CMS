using Android.Provider;
using Android.Telephony;
using SmsMessage = Microsoft.Maui.ApplicationModel.Communication.SmsMessage;

namespace MobileAuth;

public partial class CustomSms
{
    public partial void Send()
    {
        try
        {
            if ( !string.IsNullOrEmpty(Recipient) && 
                 !string.IsNullOrEmpty(Content)) 
            {
                var currentActivity = Platform.CurrentActivity;
                var smsMangerObj = currentActivity?
                    .GetSystemService(
                        Java.Lang.Class.FromType(
                            typeof(SmsManager)));

                if (smsMangerObj != null)
                {
                    SmsManager smsManager = (SmsManager)smsMangerObj;
                    smsManager.SendTextMessage(
                        Recipient,
                        null,
                        Content,
                        null,
                        null);
                }
                else
                {
                    try
                    { 
                        SmsManager smsManager = SmsManager.Default;
                        smsManager?.SendTextMessage(
                            Recipient,
                            null,
                            Content,
                            null,
                            null);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}