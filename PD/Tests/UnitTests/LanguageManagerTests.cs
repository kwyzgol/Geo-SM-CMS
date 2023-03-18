namespace Tests.UnitTests;

[TestFixture]
public class LanguageManagerTests
{
    [Test]
    public void GetSupportedCultures_GetArray_NotEmpty()
    {
        var languageManager = new LanguageManager();

        var result = languageManager.GetSupportedCultures();

        Assert.IsNotEmpty(result);
    }

    [Test]
    public void GetLocalizationOptions_GetOptions_NotNull()
    {
        var languageManager = new LanguageManager();

        var result = languageManager.GetLocalizationOptions();

        Assert.IsNotNull(result);
    }

    [Test]
    public void GetSupportedCultures_English_Exists()
    {
        var languageManager = new LanguageManager();

        var result = languageManager.GetSupportedCultures();

        Assert.Contains("en", result);
    }

    [Test]
    public void GetSupportedCultures_Polish_Exists()
    {
        var languageManager = new LanguageManager();

        var result = languageManager.GetSupportedCultures();

        Assert.Contains("pl", result);
    }
}