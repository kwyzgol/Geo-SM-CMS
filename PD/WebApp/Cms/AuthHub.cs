namespace WebApp.Cms;
public class AuthHub : Hub
{
    public static bool Connected => _connectedId != null;
    public static string AccessKey { set; private get; } = "";
    
    private static string? _connectedId;

    public async Task Connect(string accessKey)
    {
        if (AccessKey.Equals(accessKey))
        {
            if (_connectedId != null)
            {
                string connectionIdCopy = _connectedId;
                await Groups.RemoveFromGroupAsync(_connectedId, "Auth");
                _connectedId = null;
                await Clients.Client(connectionIdCopy).SendCoreAsync("Disconnect", new object[] { });
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, "Auth");
            _connectedId = Context.ConnectionId;
        }
        else Context.Abort();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);

        if (Context.ConnectionId.Equals(_connectedId))
        {
            await Groups.RemoveFromGroupAsync(_connectedId, "Auth");
            _connectedId = null;
        }
    }
}

