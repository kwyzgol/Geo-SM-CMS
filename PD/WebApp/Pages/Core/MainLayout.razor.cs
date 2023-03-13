using System.Globalization;

namespace WebApp.Pages.Core;

public partial class MainLayout
{
    private ViewMode _view = ViewMode.Best24;
    private string _latitudeInput = "";
    private string _longitudeInput = "";
    private string LatitudeInput
    {
        get => _latitudeInput;
        set
        {
            _latitudeInput = value;
            if (double.TryParse(_latitudeInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude))
            {
                Latitude = latitude;
            }
        }
    }
    private string LongitudeInput
    {
        get => _longitudeInput;
        set
        {
            _longitudeInput = value;
            if (double.TryParse(_longitudeInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
            {
                Longitude = longitude;
            }
        }
    }

    public MainLayout? CurrentPage { get; set; }
    public LoginInfo LoginInfo { get; set; } = new LoginInfo();
    public string ReCaptchaToken { get; set; } = "";
    public ReCaptchaClient? ReCaptchaClient { get; set; }
    public double Latitude { get; set; } = 52.24;
    public double Longitude { get; set; } = 21.00;


    public ViewMode View
    {
        get => _view;
        set
        {
            _view = value;
            if (ProtectedLocalStorage != null)
            {
                ProtectedLocalStorage.SetAsync("View", CmsUtilities.ViewModeToString(value));
            }
        }
    }

    [Inject] public IJSRuntime? JsRuntime { get; set; }
    [Inject] public IHubContext<AuthHub>? Auth { get; set; }
    [Inject] public ProtectedLocalStorage? ProtectedLocalStorage { get; set; }
    [Inject] public NavigationManager? Navigation { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        CurrentPage = this;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            JsRuntime?.InvokeVoidAsync("GetGeolocation");

            if (ProtectedLocalStorage != null)
            {

                var getViewMode = await ProtectedLocalStorage.GetAsync<string>("View");
                if (getViewMode.Success && getViewMode.Value != null)
                {
                    View = CmsUtilities.ViewModeToEnum(getViewMode.Value);

                    await StateChangedAsync();
                }

                var getAccessToken = await ProtectedLocalStorage.GetAsync<string>("AccessToken");
                if (getAccessToken.Success && getAccessToken.Value != null)
                {
                    bool accessKeyInit = await LoginInfo.Init(getAccessToken.Value);

                    LoginInfo.AlreadyChecked = true;

                    if (accessKeyInit.Equals(false))
                    {
                        await ProtectedLocalStorage.DeleteAsync("AccessToken");
                        Navigation?.NavigateTo("/", true);
                    }

                    await StateChangedAsync();
                }
                else
                {
                    LoginInfo.AlreadyChecked = true;
                    await StateChangedAsync();
                }
            }
        }
    }

    public void StateChanged()
    {
        StateHasChanged();
    }
    public async Task StateChangedAsync()
    {
        await this.InvokeAsync(StateHasChanged);
    }

    public async Task<bool> SendSms(string recipient, string content)
    {
        try
        {
            if (AuthHub.Connected && Auth != null)
            {
                await Auth.Clients
                    .Group("Auth")
                    .SendCoreAsync("SendSms", new object[]
                    {
                        recipient,
                        content
                    });
                return true;
            }
            else
            {
                Console.WriteLine("Device probably not connected to AuthHub.");
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }

    }
    public async Task<bool> SendAuthSms(string recipient, int code)
    {
        return await SendSms(recipient, $"[{Cms.Cms.System?.SystemName}] Your verification code: {code}");
    }
}

