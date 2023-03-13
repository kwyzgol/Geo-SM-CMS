namespace WebApp.Cms;

public class MessageHub : Hub
{
    public static string AccessKey { get; } = Guid.NewGuid().ToString();

    public async Task Connect(string accessKey, ulong userId, ulong recipientId)
    {
        if (AccessKey.Equals(accessKey))
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"{userId}/{recipientId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public async Task SendMessage(string accessKey, ulong userId, ulong recipientId, string content)
    {
        if (AccessKey.Equals(accessKey))
        {
            try
            {
                var writeToDb = MessageModel.Send(userId, recipientId, content);
                if (writeToDb is { Status: true, Result: ulong })
                {
                    var messageId = (ulong)writeToDb.Result;

                    await Clients.Group($"{userId}/{recipientId}")
                        .SendCoreAsync("ReceiveMessageSelf", new object[] { messageId, userId, recipientId, content });

                    if (recipientId != userId) 
                        await Clients.Group($"{recipientId}/{userId}")
                            .SendCoreAsync("ReceiveMessage", new object[] { messageId, userId, recipientId, content });
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}

