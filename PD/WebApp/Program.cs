if (await Cms.Init())
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorPages(razorOptions =>
    {
        razorOptions.RootDirectory = "/Pages/Core";
    });
    builder.Services.AddServerSideBlazor();
    builder.Services.AddLocalization();
    builder.Services.AddBlazoredModal();
    builder.Services.AddSignalR();

    var app = builder.Build();

    app.UseStaticFiles();
    app.UseRouting();
    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.MapHub<AuthHub>("/auth-hub");
    app.MapHub<MessageHub>("/message-hub");

    if (Cms.Language != null)
    {
        app.MapGet("set-language", Cms.Language.SetLanguage);
        app.UseRequestLocalization(Cms.Language.GetLocalizationOptions());
    }

    app.Run();
}
else
{
    Console.WriteLine("Couldn't run the application.");
}

