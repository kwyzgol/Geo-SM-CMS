namespace WebApp.Cms;

public class LanguageManager
{
    public string[] SupportedCultures { get; }

    public LanguageManager()
    {
        SupportedCultures = GetSupportedCultures();
    }

    public string[] GetSupportedCultures()
    {
        ResourceManager resourceManager = new ResourceManager(typeof(Translations));
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        List<string> supportedCulturesList = new List<string>();

        foreach (var culture in cultures)
        {
            ResourceSet? resourceSet = resourceManager.GetResourceSet(culture, true, false);
            if (resourceSet != null)
            {
                supportedCulturesList.Add(culture.Name);
            }
        }

        if (supportedCulturesList.Contains("en"))
        {
            supportedCulturesList.Remove("en");
            supportedCulturesList.Insert(0, "en");
        }

        if (supportedCulturesList.Count > 1 && supportedCulturesList.Contains(""))
        {
            supportedCulturesList.Remove("");
        }

        return supportedCulturesList.ToArray();
    }

    public RequestLocalizationOptions GetLocalizationOptions()
    {
        return new RequestLocalizationOptions()
            .SetDefaultCulture(SupportedCultures[0])
            .AddSupportedCultures(SupportedCultures)
            .AddSupportedUICultures(SupportedCultures);
    }

    public IResult SetLanguage(HttpContext context, string culture, string redirect)
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture, culture)));

        return Results.Redirect(redirect);
    }
}