namespace Tests.UnitTests;

[TestFixture]
public class CmsUtilitiesTests
{
    [Test]
    [TestCase("admin", Role.Admin)]
    [TestCase("moderator", Role.Moderator)]
    [TestCase("fact checker", Role.FactChecker)]
    [TestCase("user", Role.User)]
    [TestCase("undefined", Role.User)]
    [TestCase("", Role.User)]
    [TestCase(null, Role.User)]
    public void RoleToEnum_ProvidedString_ReturnsRole(string param, Role expected)
    {
        var result = CmsUtilities.RoleToEnum(param);

        //Assert.AreEqual(expected, result);
        Assert.Fail();
    }

    [Test]
    [TestCase(Role.Admin, "admin")]
    [TestCase(Role.Moderator, "moderator")]
    [TestCase(Role.FactChecker, "fact checker")]
    [TestCase(Role.User, "user")]
    public void RoleToString_ProvidedRole_ReturnsString(Role param, string expected)
    {
        var result = CmsUtilities.RoleToString(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("moderator", ReportType.Moderator)]
    [TestCase("fact checker", ReportType.FactChecker)]
    [TestCase("undefined", ReportType.Moderator)]
    [TestCase("", ReportType.Moderator)]
    [TestCase(null, ReportType.Moderator)]
    public void ReportTypeToEnum_ProvidedString_ReturnsReportType(string param, ReportType expected)
    {
        var result = CmsUtilities.ReportTypeToEnum(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(ReportType.Moderator, "moderator")]
    [TestCase(ReportType.FactChecker, "fact checker")]
    public void ReportTypeToString_ProvidedReportType_ReturnsString(ReportType param, string expected)
    {
        var result = CmsUtilities.ReportTypeToString(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("none", AuthType.None)]
    [TestCase("email", AuthType.Email)]
    [TestCase("sms", AuthType.Sms)]
    [TestCase("email and sms", AuthType.EmailAndSms)]
    [TestCase("undefined", AuthType.None)]
    [TestCase("", AuthType.None)]
    [TestCase(null, AuthType.None)]
    public void AuthTypeToEnum_ProvidedString_ReturnsAuthType(string param, AuthType expected)
    {
        var result = CmsUtilities.AuthTypeToEnum(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(AuthType.None, "none")]
    [TestCase(AuthType.Email, "email")]
    [TestCase(AuthType.Sms, "sms")]
    [TestCase(AuthType.EmailAndSms, "email and sms")]
    public void AuthTypeToString_ProvidedAuthType_ReturnsString(AuthType param, string expected)
    {
        var result = CmsUtilities.AuthTypeToString(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [Repeat(100)]
    public void GetRandomAuthCode_RandomResult_IsInteger()
    {
        var result = CmsUtilities.GetRandomAuthCode();

        Assert.AreEqual(typeof(int), result.GetType());
    }

    [Test]
    [Repeat(100)]
    public void GetRandomAuthCode_RandomResult_Above100000Below999999()
    {
        var result = CmsUtilities.GetRandomAuthCode();

        Assert.IsTrue(result >= 100000 && result <= 999999);
    }

    [Test]
    [TestCase(ViewMode.New, "new")]
    [TestCase(ViewMode.Best24, "best24")]
    public void ViewModeToString_ProvidedViewMode_ReturnsString(ViewMode param, string expected)
    {
        var result = CmsUtilities.ViewModeToString(param);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("new", ViewMode.New)]
    [TestCase("best24", ViewMode.Best24)]
    [TestCase("undefined", ViewMode.Best24)]
    [TestCase("", ViewMode.Best24)]
    [TestCase(null, ViewMode.Best24)]
    public void ViewModeToEnum_ProvidedString_ReturnsViewMode(string param, ViewMode expected)
    {
        var result = CmsUtilities.ViewModeToEnum(param);

        Assert.AreEqual(expected, result);
    }
}
