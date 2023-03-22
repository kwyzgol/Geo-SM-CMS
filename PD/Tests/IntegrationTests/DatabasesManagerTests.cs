using System.Diagnostics;
using System.Threading.Channels;

namespace Tests.IntegrationTests;

[TestFixture]
public class DatabasesManagerTests
{
    private static DatabasesManager _databasesManager = new DatabasesManager(
        "localhost",
        "db",
        "root",
        "PLACEHOLDER_MYSQL_ROOT_PASSWORD",
        "localhost:7687",
        "neo4j",
        "PLACEHOLDER_NEO4J_AUTH");

    private static bool _connected = false;
    private static bool _mySqlInit = false;
    private static bool _neo4jInit = false;
    private static bool _mySqlInsert = false;

    [OneTimeSetUp]
    public async Task SetUpFixture()
    {
        var delay = (int)(TimeSpan.FromSeconds(1).TotalMilliseconds);
        for (int i = 0; i < TimeSpan.FromMinutes(5).TotalMilliseconds; i += delay)
        {
            var testConnection = await _databasesManager.TestConnection();

            if (testConnection.Status)
            {
                _connected = true;
                break;
            }

            await Task.Delay(delay);
        }

        if (_connected)
        {
            _mySqlInit = _databasesManager.MySqlInit().Status;
            _neo4jInit = (await _databasesManager.Neo4jInit()).Status;
        }

        if (_mySqlInit)
        {
            _mySqlInsert = _databasesManager.MySqlInsert().Status;
        }
    }

    [OneTimeTearDown]
    public async Task TearDownFixture()
    {
        if (_connected) await _databasesManager.ClearDatabases();
    }

    [Test]
    [Order(0)]
    public async Task TestConnection_Connected_Success()
    {
        Assert.IsTrue(_connected);
    }

    [Test]
    [Order(1)]
    public async Task MySqlInit_MySqlInitialized_Success()
    {
        Assert.IsTrue(_mySqlInit);
    }

    [Test]
    [Order(2)]
    public async Task Neo4jInit_Neo4jInitialized_Success()
    {
        Assert.IsTrue(_neo4jInit);
    }

    [Test]
    [Order(3)]
    public async Task CheckTableExist_MySqlInitialized_Exists()
    {
        if (_connected == false || _mySqlInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var checkTableExist = _databasesManager.CheckTableExist();
        Assert.IsTrue(checkTableExist.Status);
    }

    [Test]
    [Order(4)]
    public async Task MySqlInsert_InitialSettingsInserted_Success()
    {
        Assert.IsTrue(_mySqlInsert);
    }

    [Test]
    [Order(5)]
    public async Task SearchPlace_KatowiceSpodek_CorrectPlace()
    {
        if (_connected == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var searchPlace = await _databasesManager.SearchPlace("Katowice Spodek");
        if (searchPlace == null
            || searchPlace.Status == false
            || searchPlace.Result is not List<PlaceModel>
            || (searchPlace.Result is List<PlaceModel>
                && ((List<PlaceModel>)searchPlace.Result).Count.Equals(0)))
        {
            Assert.Fail("SearchPlace returned false status or invalid (or empty) result.");
            return;
        }

        var maxAcceptedDifference = 0.01;

        var estimatedLatitude = 50.266;
        var estimatedLongitude = 19.025;

        var place = ((List<PlaceModel>)searchPlace.Result)[0];

        var latitudeDifference = Math.Abs(place.Latitude - estimatedLatitude);
        var longitudeDifference = Math.Abs(place.Longitude - estimatedLongitude);

        var result = latitudeDifference <= maxAcceptedDifference
                     && longitudeDifference <= maxAcceptedDifference;

        Assert.IsTrue(result);
    }

    [Test]
    [Order(6)]
    public async Task Search_NonExistentData_EmptyList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var search = await _databasesManager.Search(SearchType.User, "T6_User");

        if (search == null
            || search.Status == false
            || search.Result is not List<UserModel>)
        {
            Assert.Fail("Search returned false status or invalid result.");
            return;
        }

        var result = (List<UserModel>)search.Result;
        Assert.AreEqual(result.Count, 0);
    }

    [Test]
    [Order(7)]
    public async Task Search_SampleUsers_ReturnsUserModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T7_User";
        user1.Password = "T7_Password";
        user1.ConfirmPassword = "T7_Password";
        user1.TermsOfService = true;
        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T7_User_StartsWith";
        user2.Password = "T7_Password";
        user2.ConfirmPassword = "T7_Password";
        user2.TermsOfService = true;
        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var user3 = new RegistrationModel();
        user3.Username = "Containing_T7_User";
        user3.Password = "T7_Password";
        user3.ConfirmPassword = "T7_Password";
        user3.TermsOfService = true;
        if (user3.IsValid)
        {
            var register = _databasesManager.Register(user3);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var search = await _databasesManager.Search(SearchType.User, "T7_User");

        if (search == null
            || search.Status == false
            || search.Result is not List<UserModel>)
        {
            Assert.Fail("Search returned false status or invalid result.");
            return;
        }

        var result = (List<UserModel>)search.Result;

        var resultCount = result.Count == 3;
        var resultExact = result[0]?.Username?.Equals("T7_User") ?? false;
        var resultStartsWith = result[1]?.Username?.Equals("T7_User_StartsWith") ?? false;
        var resultContains = result[2]?.Username?.Equals("Containing_T7_User") ?? false;

        Assert.IsTrue(resultCount && resultExact && resultStartsWith && resultContains);
    }

    [Test]
    [Order(8)]
    public async Task Search_SampleTags_ReturnsStringList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T8_User";
        user.Password = "T8_Password";
        user.ConfirmPassword = "T8_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T8_Title";
        newPost.Content = "T8_Content #T8_Tag #T8_Tag_StartsWith #Containing_T8_Tag";

        List<string> tags = new List<string>();
        tags.Add("T8_Tag");
        tags.Add("T8_Tag_StartsWith");
        tags.Add("Containing_T8_Tag");

        await _databasesManager.CreatePost(accessToken, newPost, tags, null);

        var search = await _databasesManager.Search(SearchType.Tag, "T8_Tag");

        if (search == null
            || search.Status == false
            || search.Result is not List<string>)
        {
            Assert.Fail("Search returned false status or invalid result.");
            return;
        }

        var result = (List<string>)search.Result;

        var resultCount = result.Count == 3;
        var resultExact = result[0]?.Equals("T8_Tag") ?? false;
        var resultStartsWith = result[1]?.Equals("T8_Tag_StartsWith") ?? false;
        var resultContains = result[2]?.Equals("Containing_T8_Tag") ?? false;

        Assert.IsTrue(resultCount && resultExact && resultStartsWith && resultContains);
    }

    [Test]
    [Order(9)]
    public async Task Search_SamplePosts_ReturnsPostModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T9_User";
        user.Password = "T9_Password";
        user.ConfirmPassword = "T9_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost1 = new PostModel();
        newPost1.Title = "T9_Title";
        newPost1.Content = "T9_Content";

        var newPost2 = new PostModel();
        newPost2.Title = "T9_Title_StartsWith";
        newPost2.Content = "T9_Content";

        var newPost3 = new PostModel();
        newPost3.Title = "Containing_T9_Title";
        newPost3.Content = "T9_Content";

        List<string> tags = new List<string>();

        await _databasesManager.CreatePost(accessToken, newPost1, tags, null);
        await _databasesManager.CreatePost(accessToken, newPost2, tags, null);
        await _databasesManager.CreatePost(accessToken, newPost3, tags, null);

        var search = await _databasesManager.Search(SearchType.Post, "T9_Title");

        if (search == null
            || search.Status == false
            || search.Result is not List<PostModel>)
        {
            Assert.Fail("Search returned false status or invalid result.");
            return;
        }

        var result = (List<PostModel>)search.Result;

        var resultCount = result.Count == 3;
        var resultExact = result[0]?.Title.Equals("T9_Title") ?? false;
        var resultStartsWith = result[1]?.Title.Equals("T9_Title_StartsWith") ?? false;
        var resultContains = result[2]?.Title.Equals("Containing_T9_Title") ?? false;

        Assert.IsTrue(resultCount && resultExact && resultStartsWith && resultContains);
    }

    [Test]
    [Order(10)]
    public async Task CheckAdminExists_NoAdmin_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.CheckAdminExists();

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(11)]
    public async Task CheckAdminExists_AdminExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var newAdmin = new RegistrationModel();
        newAdmin.Username = "T11_User";
        newAdmin.Password = "T11_Password";
        newAdmin.ConfirmPassword = "T11_Password";
        newAdmin.TermsOfService = true;
        if (newAdmin.IsValid) _databasesManager.Register(newAdmin, true);

        var result = _databasesManager.CheckAdminExists();

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(12)]
    public async Task GetSettings_GetInitialSettings_Success()
    {
        if (_connected == false
            || _mySqlInit == false
            || _neo4jInit == false
            || _mySqlInsert == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager
            .GetSettings(new GeneralSettings(), new AuthManager(), new ReCaptchaManager());

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(13)]
    public async Task UpdateSettings_ChangeSystemName_DataChanged()
    {
        if (_connected == false
            || _mySqlInit == false
            || _neo4jInit == false
            || _mySqlInsert == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T13_User";
        user.Password = "T13_Password";
        user.ConfirmPassword = "T13_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user, true);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var generalSetting = new GeneralSettings();
        generalSetting.SystemName = "T13_SystemName";

        await _databasesManager
            .UpdateSettings(
                accessToken,
                generalSetting,
                new AuthManager(),
                new ReCaptchaManager());

        var result = new GeneralSettings();
        _databasesManager.GetSettings(result, new AuthManager(), new ReCaptchaManager());
        Assert.AreEqual("T13_SystemName", result.SystemName);
    }

    [Test]
    [Order(14)]
    public async Task CheckUsernameExists_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.CheckUsernameExists("T14_User");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(15)]
    public async Task CheckUsernameExists_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T15_User";
        user.Password = "T15_Password";
        user.ConfirmPassword = "T15_Password";
        user.TermsOfService = true;
        if (user.IsValid) _databasesManager.Register(user);

        var result = _databasesManager.CheckUsernameExists("T15_User");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(16)]
    public async Task CheckEmailExists_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.CheckEmailExists("T16-User@T16-Email.com");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(17)]
    public async Task CheckEmailExists_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelEmail();
        user.Username = "T17_User";
        user.Password = "T17_Password";
        user.ConfirmPassword = "T17_Password";
        user.TermsOfService = true;
        user.Email = "T17-User@T17-Email.com";
        if (user.IsValid) _databasesManager.Register(user);

        var result = _databasesManager.CheckEmailExists("T17-User@T17-Email.com");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(18)]
    public async Task CheckPhoneNumberExists_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.CheckPhoneNumberExists("+00", "181818");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(19)]
    public async Task CheckPhoneNumberExists_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelSms();
        user.Username = "T19_User";
        user.Password = "T19_Password";
        user.ConfirmPassword = "T19_Password";
        user.TermsOfService = true;
        user.PhoneCountry = "+00";
        user.PhoneNumber = "191919";
        if (user.IsValid) _databasesManager.Register(user);

        var result = _databasesManager.CheckPhoneNumberExists("+00", "191919");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(20)]
    public async Task Register_NewUser_ReturnsId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T20_User";
        user.Password = "T20_Password";
        user.ConfirmPassword = "T20_Password";
        user.TermsOfService = true;
        var result = _databasesManager.Register(user);

        Assert.IsTrue(result.Status && result.Result is ulong);
    }

    [Test]
    [Order(21)]
    public async Task GetUserId_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.GetUserId("T21_User");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(22)]
    public async Task GetUserId_UserExists_ReturnsId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T22_User";
        user.Password = "T22_Password";
        user.ConfirmPassword = "T22_Password";
        user.TermsOfService = true;
        if (user.IsValid) _databasesManager.Register(user);

        var result = _databasesManager.GetUserId("T22_User");

        Assert.IsTrue(result.Status && result.Result is ulong);
    }

    [Test]
    [Order(23)]
    public async Task GetUserIdFromAccessToken_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.GetUserIdFromAccessToken("no-user");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(24)]
    public async Task GetUserIdFromAccessToken_UserExists_ReturnsId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T24_User";
        user.Password = "T24_Password";
        user.ConfirmPassword = "T24_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = _databasesManager.GetUserIdFromAccessToken(accessToken);

        Assert.IsTrue(result.Status && result.Result is ulong);
    }

    [Test]
    [Order(25)]
    public async Task GetUserStatus_NewUser_ReturnsRegistered()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T25_User";
        user.Password = "T25_Password";
        user.ConfirmPassword = "T25_Password";
        user.TermsOfService = true;

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
            }
        }

        string status = "";

        if (userId != null)
        {
            var getUserStatus = _databasesManager.GetUserStatus((ulong)userId);
            if (getUserStatus.Status && getUserStatus.Result is string)
            {
                status = (string)getUserStatus.Result;
            }
        }

        Assert.AreEqual("registered", status);
    }

    [Test]
    [Order(26)]
    public async Task GetUserStatus_ActiveUser_ReturnsActive()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T26_User";
        user.Password = "T26_Password";
        user.ConfirmPassword = "T26_Password";
        user.TermsOfService = true;

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate((ulong)userId);
            }
        }

        string status = "";

        if (userId != null)
        {
            var getUserStatus = _databasesManager.GetUserStatus((ulong)userId);
            if (getUserStatus.Status && getUserStatus.Result is string)
            {
                status = (string)getUserStatus.Result;
            }
        }

        Assert.AreEqual("active", status);
    }

    [Test]
    [Order(27)]
    public async Task GetUserStatus_BannedUser_ReturnsBanned()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T27_User";
        user.Password = "T27_Password";
        user.ConfirmPassword = "T27_Password";
        user.TermsOfService = true;

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate((ulong)userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T27_Admin";
        admin.Password = "T27_Password";
        admin.ConfirmPassword = "T27_Password";
        admin.TermsOfService = true;
        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.BanUser(adminAccessToken, "T27_User", "T27_Reason", 1);

        string status = "";

        if (userId != null)
        {
            var getUserStatus = _databasesManager.GetUserStatus((ulong)userId);
            if (getUserStatus.Status && getUserStatus.Result is string)
            {
                status = (string)getUserStatus.Result;
            }
        }

        Assert.AreEqual("banned", status);
    }

    [Test]
    [Order(28)]
    public async Task GetUserStatusFromAccessToken_InvalidToken_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var result = _databasesManager.GetUserStatusFromAccessToken("no-user");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(29)]
    public async Task GetUserStatusFromAccessToken_ValidToken_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T29_User";
        user.Password = "T29_Password";
        user.ConfirmPassword = "T29_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var status = "";

        var getStatus = _databasesManager.GetUserStatusFromAccessToken(accessToken);

        if (getStatus.Status && getStatus.Result is string)
        {
            status = (string)getStatus.Result;
        }

        Assert.AreEqual("active", status);
    }

    [Test]
    [Order(30)]
    public async Task GetUserStatusFromAccessToken_RoleAdminTokenUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T30_User";
        user.Password = "T30_Password";
        user.ConfirmPassword = "T30_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager
            .GetUserStatusFromAccessToken(accessToken, Role.Admin);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(31)]
    public async Task GetUserStatusFromAccessToken_RoleAdminTokenAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T31_User";
        user.Password = "T31_Password";
        user.ConfirmPassword = "T31_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user, true);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var status = "";

        var result = await _databasesManager
            .GetUserStatusFromAccessToken(accessToken, Role.Admin);

        if (result.Status && result.Result is string)
        {
            status = (string)result.Result;
        }

        Assert.IsTrue(result.Status && status.Equals("active"));
    }

    [Test]
    [Order(32)]
    public async Task GetUserAuthType_None_ProperEnum()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T32_User";
        user.Password = "T32_Password";
        user.ConfirmPassword = "T32_Password";
        user.TermsOfService = true;

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserAuthType = userId != null ? _databasesManager.GetUserAuthType((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserAuthType.Status;
        var resultCorrectType = getUserAuthType.Result is AuthType;
        var resultCorrectValue = getUserAuthType.Result is AuthType
                                 && (AuthType)getUserAuthType.Result == AuthType.None;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(33)]
    public async Task GetUserAuthType_Email_ProperEnum()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelEmail();

        user.Username = "T33_User";
        user.Password = "T33_Password";
        user.ConfirmPassword = "T33_Password";
        user.TermsOfService = true;
        user.Email = "T33-User@T33-Email.com";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserAuthType = userId != null ? _databasesManager.GetUserAuthType((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserAuthType.Status;
        var resultCorrectType = getUserAuthType.Result is AuthType;
        var resultCorrectValue = getUserAuthType.Result is AuthType
                                 && (AuthType)getUserAuthType.Result == AuthType.Email;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(34)]
    public async Task GetUserAuthType_Sms_ProperEnum()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelSms();

        user.Username = "T34_User";
        user.Password = "T34_Password";
        user.ConfirmPassword = "T34_Password";
        user.TermsOfService = true;
        user.PhoneCountry = "+00";
        user.PhoneNumber = "343434";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserAuthType = userId != null ? _databasesManager.GetUserAuthType((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserAuthType.Status;
        var resultCorrectType = getUserAuthType.Result is AuthType;
        var resultCorrectValue = getUserAuthType.Result is AuthType
                                 && (AuthType)getUserAuthType.Result == AuthType.Sms;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(35)]
    public async Task GetUserAuthType_EmailAndSms_ProperEnum()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T35_User";
        user.Password = "T35_Password";
        user.ConfirmPassword = "T35_Password";
        user.TermsOfService = true;
        user.Email = "T35-User@T35-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "353535";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserAuthType = userId != null ? _databasesManager.GetUserAuthType((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserAuthType.Status;
        var resultCorrectType = getUserAuthType.Result is AuthType;
        var resultCorrectValue = getUserAuthType.Result is AuthType
                                 && (AuthType)getUserAuthType.Result == AuthType.EmailAndSms;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(36)]
    public async Task CreateAuthCodes_GetAuthCodes_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T36_User";
        user.Password = "T36_Password";
        user.ConfirmPassword = "T36_Password";
        user.TermsOfService = true;
        user.Email = "T36-User@T36-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "363636";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var createAuthCodes = userId != null ?
            _databasesManager.CreateAuthCodes((ulong)userId, AuthType.EmailAndSms)
            : new OperationResult(false);

        Assert.IsTrue(createAuthCodes is { Status: true, Result: (int, int) });
    }

    [Test]
    [Order(37)]
    public async Task GetUserContact_None_ReturnsNull()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();

        user.Username = "T37_User";
        user.Password = "T37_Password";
        user.ConfirmPassword = "T37_Password";
        user.TermsOfService = true;

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserContact = userId != null ?
            _databasesManager.GetUserContact((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserContact.Status;
        var resultCorrectType = getUserContact.Result is ValueTuple<string?, string?, string?>;
        var resultCorrectValue = getUserContact.Result is ValueTuple<string?, string?, string?>
                                 && (ValueTuple<string?, string?, string?>)getUserContact.Result == (null, null, null);
        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(38)]
    public async Task GetUserContact_Full_ReturnsData()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T38_User";
        user.Password = "T38_Password";
        user.ConfirmPassword = "T38_Password";
        user.TermsOfService = true;
        user.Email = "T38-User@T38-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "383838";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var getUserContact = userId != null ?
            _databasesManager.GetUserContact((ulong)userId)
            : new OperationResult(false);

        var resultStatus = getUserContact.Status;
        var resultCorrectType = getUserContact.Result is ValueTuple<string?, string?, string?>;

        var resultCorrectValue = false;

        if (resultCorrectType && getUserContact.Result != null)
        {
            var value = (ValueTuple<string?, string?, string?>)getUserContact.Result;

            resultCorrectValue = value.Item1 == user.Email &&
                                 value.Item2 == user.PhoneCountry &&
                                 value.Item3 == user.PhoneNumber;
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(39)]
    public async Task CheckAuthCode_TypeEmail_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T39_User";
        user.Password = "T39_Password";
        user.ConfirmPassword = "T39_Password";
        user.TermsOfService = true;
        user.Email = "T39-User@T39-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "393939";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var createAuthCodes = userId != null ?
            _databasesManager.CreateAuthCodes((ulong)userId, AuthType.EmailAndSms)
            : new OperationResult(false);

        var authCodes = createAuthCodes is { Status: true, Result: (int, int) }
            ? (ValueTuple<int, int>)createAuthCodes.Result
            : (0, 0);

        var result = userId != null ?
            _databasesManager.CheckAuthCode((ulong)userId, authCodes.Item1, "email")
            : new OperationResult(false);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(40)]
    public async Task CheckAuthCode_TypeEmail_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T40_User";
        user.Password = "T40_Password";
        user.ConfirmPassword = "T40_Password";
        user.TermsOfService = true;
        user.Email = "T40-User@T40-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "404040";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var result = userId != null ?
            _databasesManager.CheckAuthCode((ulong)userId, 123456, "email")
            : new OperationResult(false);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(41)]
    public async Task CheckAuthCode_TypeSms_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T41_User";
        user.Password = "T41_Password";
        user.ConfirmPassword = "T41_Password";
        user.TermsOfService = true;
        user.Email = "T41-User@T41-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "414141";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var createAuthCodes = userId != null ?
            _databasesManager.CreateAuthCodes((ulong)userId, AuthType.EmailAndSms)
            : new OperationResult(false);

        var authCodes = createAuthCodes is { Status: true, Result: (int, int) }
            ? (ValueTuple<int, int>)createAuthCodes.Result
            : (0, 0);

        var result = userId != null ?
            _databasesManager.CheckAuthCode((ulong)userId, authCodes.Item2, "sms")
            : new OperationResult(false);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(42)]
    public async Task CheckAuthCode_TypeSms_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModelBoth();

        user.Username = "T42_User";
        user.Password = "T42_Password";
        user.ConfirmPassword = "T42_Password";
        user.TermsOfService = true;
        user.Email = "T42-User@T42-Email.com";
        user.PhoneCountry = "+00";
        user.PhoneNumber = "424242";

        ulong? userId = null;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status) userId = (ulong)register.Result!;
        }

        var result = userId != null ?
            _databasesManager.CheckAuthCode((ulong)userId, 123456, "sms")
            : new OperationResult(false);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(43)]
    public async Task Activate_NewUser_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T43_User";
        user.Password = "T43_Password";
        user.ConfirmPassword = "T43_Password";
        user.TermsOfService = true;

        var result = new OperationResult(false);

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                result = await _databasesManager.Activate(userId);
            }
        }

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(44)]
    public async Task Login_ActiveUser_ReturnsAccessToken()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T44_User";
        user.Password = "T44_Password";
        user.ConfirmPassword = "T44_Password";
        user.TermsOfService = true;

        var login = new OperationResult(false);

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                login = _databasesManager.Login(userId);
            }
        }

        Assert.IsTrue(login is { Status: true, Result: string });
    }

    [Test]
    [Order(45)]
    public async Task Login_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var login = _databasesManager.Login(9999);

        Assert.IsFalse(login.Status);
    }

    [Test]
    [Order(46)]
    public async Task GetUser_UserId_ReturnsUserModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T46_User";
        user.Password = "T46_Password";
        user.ConfirmPassword = "T46_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var result = await _databasesManager.GetUser(userId);

        Assert.IsTrue(result is { Status: true, Result: UserModel });
    }

    [Test]
    [Order(47)]
    public async Task GetUser_Username_ReturnsUserModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T47_User";
        user.Password = "T47_Password";
        user.ConfirmPassword = "T47_Password";
        user.TermsOfService = true;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var result = await _databasesManager.GetUser(user.Username);

        Assert.IsTrue(result is { Status: true, Result: UserModel });
    }

    [Test]
    [Order(48)]
    public async Task GetUserFromAccessToken_ValidToken_ReturnsUserModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T48_User";
        user.Password = "T48_Password";
        user.ConfirmPassword = "T48_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.GetUserFromAccessToken(accessToken);

        Assert.IsTrue(result is { Status: true, Result: UserModel });
    }

    [Test]
    [Order(49)]
    public async Task ChangePassword_ActiveUser_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T49_User";
        user.Password = "T49_Password";
        user.ConfirmPassword = "T49_Password";
        user.TermsOfService = true;

        ulong userId = 9999;


        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var result = _databasesManager.ChangePassword(userId, "T49_NewPassword");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(50)]
    public async Task CheckLoginData_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T50_User";
        user.Password = "T50_Password";
        user.ConfirmPassword = "T50_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var checkLoginData = _databasesManager
            .CheckLoginData(user.Username, user.Password);

        var resultId = (ulong?)checkLoginData.Result;

        Assert.IsTrue(checkLoginData is { Status: true, Result: ulong } && userId == resultId);
    }

    [Test]
    [Order(51)]
    public async Task CheckLoginData_UserExistsWrongPassword_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T51_User";
        user.Password = "T51_Password";
        user.ConfirmPassword = "T51_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var checkLoginData = _databasesManager
            .CheckLoginData(user.Username, "T51_WrongPassword");

        var resultId = (ulong?)checkLoginData.Result;

        Assert.IsFalse(checkLoginData is { Status: true, Result: ulong } && userId == resultId);
    }

    [Test]
    [Order(52)]
    public async Task CheckLoginData_NoUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var checkLoginData = _databasesManager
            .CheckLoginData("T52_User", "T52_Password");

        var resultId = (ulong?)checkLoginData.Result;

        Assert.IsFalse(checkLoginData.Status);
    }

    [Test]
    [Order(53)]
    public async Task RevokeAccessToken_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T53_User";
        user.Password = "T53_Password";
        user.ConfirmPassword = "T53_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var before = await _databasesManager.GetUserFromAccessToken(accessToken);

        var result = _databasesManager.RevokeAccessToken(accessToken);

        var after = await _databasesManager.GetUserFromAccessToken(accessToken);

        Assert.IsTrue(result.Status
                      && before is { Status: true, Result: UserModel }
                      && after is { Status: false, Result: null });
    }

    [Test]
    [Order(54)]
    public async Task RevokeAllAccessTokens_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T54_User";
        user.Password = "T54_Password";
        user.ConfirmPassword = "T54_Password";
        user.TermsOfService = true;

        string accessToken1 = "";
        string accessToken2 = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);

                var login1 = _databasesManager.Login(userId);
                if (login1.Status && login1.Result is string)
                {
                    accessToken1 = (string)login1.Result;
                }

                var login2 = _databasesManager.Login(userId);
                if (login2.Status && login2.Result is string)
                {
                    accessToken2 = (string)login2.Result;
                }
            }
        }

        var before1 = await _databasesManager.GetUserFromAccessToken(accessToken1);
        var before2 = await _databasesManager.GetUserFromAccessToken(accessToken2);

        var result = _databasesManager.RevokeAllAccessTokens(userId);

        var after1 = await _databasesManager.GetUserFromAccessToken(accessToken1);
        var after2 = await _databasesManager.GetUserFromAccessToken(accessToken2);

        Assert.IsTrue(result.Status
                      && before1 is { Status: true, Result: UserModel }
                      && before2 is { Status: true, Result: UserModel }
                      && after1 is { Status: false, Result: null }
                      && after2 is { Status: false, Result: null });
    }

    [Test]
    [Order(55)]
    public async Task UpdatePassword_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T55_User";
        user.Password = "T55_Password";
        user.ConfirmPassword = "T55_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.UpdatePassword(accessToken, "T55_NewPassword");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(56)]
    public async Task UpdateEmail_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T56_User";
        user.Password = "T56_Password";
        user.ConfirmPassword = "T56_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.UpdateEmail(accessToken, "T56-NewEmail@T56-Email.com");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(57)]
    public async Task UpdatePhone_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T57_User";
        user.Password = "T57_Password";
        user.ConfirmPassword = "T57_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.UpdatePhone(accessToken, "+00", "575757");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(58)]
    public async Task UpdateAvatar_UserExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T58_User";
        user.Password = "T58_Password";
        user.ConfirmPassword = "T58_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.UpdateAvatar(accessToken, "user/T58_NewAvatar");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(59)]
    public async Task ChangeRole_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T59_User";
        user.Password = "T59_Password";
        user.ConfirmPassword = "T59_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T59_Admin";
        admin.Password = "T59_Password";
        admin.ConfirmPassword = "T59_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.ChangeRole(adminAccessToken, userId, Role.Moderator);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(60)]
    public async Task ChangeRole_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T60_User";
        user.Password = "T60_Password";
        user.ConfirmPassword = "T60_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T60_NotAdmin";
        notAdmin.Password = "T60_Password";
        notAdmin.ConfirmPassword = "T60_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.ChangeRole(notAdminAccessToken, userId, Role.Moderator);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(61)]
    public async Task DeleteUser_Self_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T61_User";
        user.Password = "T61_Password";
        user.ConfirmPassword = "T61_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.DeleteUser(accessToken);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(62)]
    public async Task DeleteUser_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T62_User";
        user.Password = "T62_Password";
        user.ConfirmPassword = "T62_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T62_Admin";
        admin.Password = "T62_Password";
        admin.ConfirmPassword = "T62_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.DeleteUser(adminAccessToken, userId);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(63)]
    public async Task DeleteUser_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T63_User";
        user.Password = "T63_Password";
        user.ConfirmPassword = "T63_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T63_NotAdmin";
        notAdmin.Password = "T63_Password";
        notAdmin.ConfirmPassword = "T63_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.DeleteUser(notAdminAccessToken, userId);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(64)]
    public async Task GetLoginHistory_UserExists_ReturnsList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T64_User";
        user.Password = "T64_Password";
        user.ConfirmPassword = "T64_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login1 = _databasesManager.Login(userId);
                var login2 = _databasesManager.Login(userId);
                var login3 = _databasesManager.Login(userId);
                var login4 = _databasesManager.Login(userId);
                var login5 = _databasesManager.Login(userId);
                var login6 = _databasesManager.Login(userId);
                var login7 = _databasesManager.Login(userId);
                var login8 = _databasesManager.Login(userId);
                var login9 = _databasesManager.Login(userId);
                var login10 = _databasesManager.Login(userId);
                if (login10.Status && login10.Result is string)
                {
                    accessToken = (string)login10.Result;
                }
            }
        }

        var getLoginHistory = _databasesManager.GetLoginHistory(userId, accessToken);

        var resultStatus = getLoginHistory.Status;
        var resultCorrectType = getLoginHistory.Result is List<(DateTime, bool)>;

        var resultCorrectCount = false;
        var resultCurrentSession = false;

        if (getLoginHistory.Result is List<(DateTime, bool)>)
        {
            var list = (List<(DateTime, bool)>)getLoginHistory.Result;
            resultCorrectCount = list.Count == 10;
            if (list.Count != 0) resultCurrentSession = list[0].Item2;
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectCount && resultCurrentSession);
    }

    [Test]
    [Order(65)]
    public async Task CreatePost_ActiveUser_ReturnsPostId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T65_User";
        user.Password = "T65_Password";
        user.ConfirmPassword = "T65_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T65_Title";
        newPost.Content = "T65_Content";

        var result = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        Assert.IsTrue(result is { Status: true, Result: ulong });
    }

    [Test]
    [Order(66)]
    public async Task CreatePost_BannedUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T66_User";
        user.Password = "T66_Password";
        user.ConfirmPassword = "T66_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T66_Title";
        newPost.Content = "T66_Content";

        var admin = new RegistrationModel();
        admin.Username = "T66_Admin";
        admin.Password = "T66_Password";
        admin.ConfirmPassword = "T66_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.BanUser(adminAccessToken, user.Username, "T66_Reason", 1);

        var result = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(67)]
    public async Task GetPost_PostExists_ReturnsPostModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T67_User";
        user.Password = "T67_Password";
        user.ConfirmPassword = "T67_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T67_Title";
        newPost.Content = "T67_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        var result = new OperationResult(false);

        if (createPost is { Status: true, Result: ulong })
        {
            var postId = (ulong)createPost.Result;
            result = await _databasesManager.GetPost((long)postId);
        }

        Assert.IsTrue(result is { Status: true, Result: PostModel });
    }

    [Test]
    [Order(68)]
    public async Task GetPosts_New_ReturnsPostModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T68_User";
        user.Password = "T68_Password";
        user.ConfirmPassword = "T68_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost1 = new PostModel();
        newPost1.Title = "T68_Title1";
        newPost1.Content = "T68_Content1 #T68";

        var newPost2 = new PostModel();
        newPost2.Title = "T68_Title2";
        newPost2.Content = "T68_Content2 #T68";

        var newPost3 = new PostModel();
        newPost3.Title = "T68_Title3";
        newPost3.Content = "T68_Content3 #T68";

        var tags = new List<string>();
        tags.Add("T68");

        await _databasesManager.CreatePost(accessToken, newPost1, tags, null);
        await _databasesManager.CreatePost(accessToken, newPost2, tags, null);
        await _databasesManager.CreatePost(accessToken, newPost3, tags, null);

        var getPosts = await _databasesManager.GetPosts(ViewMode.New, tags);

        var resultStatus = getPosts.Status;
        var resultCorrectType = getPosts.Result is List<PostModel>;

        var resultCorrectCount = false;
        var resultCorrectOrder = false;

        if (resultCorrectType && getPosts.Result != null)
        {
            var list = (List<PostModel>)getPosts.Result;
            resultCorrectCount = list.Count == 3;
            if (resultCorrectCount)
            {
                resultCorrectOrder = list[0].Title == "T68_Title3" &&
                                     list[1].Title == "T68_Title2" &&
                                     list[2].Title == "T68_Title1";
            }
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectCount && resultCorrectOrder);
    }

    [Test]
    [Order(69)]
    public async Task GetPosts_Best_ReturnsPostModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T69_User";
        user.Password = "T69_Password";
        user.ConfirmPassword = "T69_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost1 = new PostModel();
        newPost1.Title = "T69_Title1";
        newPost1.Content = "T69_Content1 #T69";

        var newPost2 = new PostModel();
        newPost2.Title = "T69_Title2";
        newPost2.Content = "T69_Content2 #T69";

        var newPost3 = new PostModel();
        newPost3.Title = "T69_Title3";
        newPost3.Content = "T69_Content3 #T69";

        var tags = new List<string>();
        tags.Add("T69");

        var post1 = await _databasesManager.CreatePost(accessToken, newPost1, tags, null);
        var post2 = await _databasesManager.CreatePost(accessToken, newPost2, tags, null);
        var post3 = await _databasesManager.CreatePost(accessToken, newPost3, tags, null);

        if (post1.Status && post1.Result is ulong)
        {
            var getPost = await _databasesManager.GetPost((long)(ulong)post1.Result);

            if (getPost.Status && getPost.Result is PostModel)
            {
                var post = (PostModel)getPost.Result;
                await _databasesManager.Upvote(post, accessToken);
            }
        }

        if (post2.Status && post2.Result is ulong)
        {
            var getPost = await _databasesManager.GetPost((long)(ulong)post2.Result);

            if (getPost.Status && getPost.Result is PostModel)
            {
                var post = (PostModel)getPost.Result;
                await _databasesManager.Downvote(post, accessToken);
            }
        }

        var getPosts = await _databasesManager.GetPosts(ViewMode.Best24, tags);

        var resultStatus = getPosts.Status;
        var resultCorrectType = getPosts.Result is List<PostModel>;

        var resultCorrectCount = false;
        var resultCorrectOrder = false;

        if (resultCorrectType && getPosts.Result != null)
        {
            var list = (List<PostModel>)getPosts.Result;
            resultCorrectCount = list.Count == 3;
            if (resultCorrectCount)
            {
                resultCorrectOrder = list[0].Title == "T69_Title1" &&
                                     list[1].Title == "T69_Title3" &&
                                     list[2].Title == "T69_Title2";
            }
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectCount && resultCorrectOrder);
    }

    [Test]
    [Order(70)]
    public async Task GetPosts_CustomTag_ReturnsPostModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T70_User";
        user.Password = "T70_Password";
        user.ConfirmPassword = "T70_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost1 = new PostModel();
        newPost1.Title = "T70_Title1";
        newPost1.Content = "70_Content1 #T70-tag1";

        var newPost2 = new PostModel();
        newPost2.Title = "T70_Title2";
        newPost2.Content = "T70_Content2 #T70-tag2";

        var tags1 = new List<string>();
        tags1.Add("T70-tag1");
        var tags2 = new List<string>();
        tags2.Add("T70-tag2");

        var post1 = await _databasesManager.CreatePost(accessToken, newPost1, tags1, null);
        var post2 = await _databasesManager.CreatePost(accessToken, newPost2, tags2, null);

        var getPosts = await _databasesManager.GetPosts(ViewMode.New, tags2);

        var resultStatus = getPosts.Status;
        var resultCorrectType = getPosts.Result is List<PostModel>;

        var resultCorrectCount = false;

        if (resultCorrectType && getPosts.Result != null)
        {
            var list = (List<PostModel>)getPosts.Result;
            resultCorrectCount = list.Count == 1;
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectCount);
    }

    [Test]
    [Order(71)]
    public async Task GetPosts_LocationKatowice_ReturnsPostModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T71_User";
        user.Password = "T71_Password";
        user.ConfirmPassword = "T71_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost1 = new PostModel();
        newPost1.Title = "T71_Title1 Katowice";
        newPost1.Content = "T71_Content1 #T71";
        var geoPoint1 = new GeoPointModel();
        geoPoint1.Latitude = 50.2593;
        geoPoint1.Longitude = 19.0221;

        var newPost2 = new PostModel();
        newPost2.Title = "T71_Title2 Katowice";
        newPost2.Content = "T71_Content2 #T71";
        var geoPoint2 = new GeoPointModel();
        geoPoint2.Latitude = 50.2661;
        geoPoint2.Longitude = 19.0260;

        var newPost3 = new PostModel();
        newPost3.Title = "T71_Title3 Chorzów";
        newPost3.Content = "T71_Content3 #T71";
        var geoPoint3 = new GeoPointModel();
        geoPoint3.Latitude = 50.2976;
        geoPoint3.Longitude = 18.9541;

        var tags = new List<string>();
        tags.Add("T71");

        await _databasesManager.CreatePost(accessToken, newPost1, tags, geoPoint1);
        await _databasesManager.CreatePost(accessToken, newPost2, tags, geoPoint2);
        await _databasesManager.CreatePost(accessToken, newPost3, tags, geoPoint3);

        var searchGeoPoint = new GeoPointModel();
        searchGeoPoint.Latitude = 50.2649;
        searchGeoPoint.Longitude = 19.0238;
        searchGeoPoint.Meters = 1000;
        var getPosts = await _databasesManager.GetPosts(ViewMode.New, tags, searchGeoPoint);

        var resultStatus = getPosts.Status;
        var resultCorrectType = getPosts.Result is List<PostModel>;

        var resultCorrectCount = false;

        if (resultCorrectType && getPosts.Result != null)
        {
            var list = (List<PostModel>)getPosts.Result;
            resultCorrectCount = list.Count == 2;
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectCount);
    }

    [Test]
    [Order(72)]
    public async Task GetTags_TagsExist_ReturnsList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T72_User";
        user.Password = "T72_Password";
        user.ConfirmPassword = "T72_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T72_Title";
        newPost.Content = "T72_Content #T72";
        var tags = new List<string>();
        tags.Add("T72");

        await _databasesManager.CreatePost(accessToken, newPost, tags, null);

        var getTags = await _databasesManager.GetTags();

        var resultStatus = getTags.Status;
        var resultCorrectType = getTags.Result is List<string>;
        var resultContainsValue = false;
        if (resultCorrectType && getTags.Result != null)
        {
            var list = (List<string>)getTags.Result;
            resultContainsValue = list.Contains("T72");
        }

        Assert.IsTrue(resultStatus && resultCorrectType && resultContainsValue);
    }

    [Test]
    [Order(73)]
    public async Task Upvote_UserExistsPostExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T73_User";
        user.Password = "T73_Password";
        user.ConfirmPassword = "T73_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T73_Title";
        newPost.Content = "T73_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var before = await _databasesManager.GetPost((long)postId, accessToken);

        var postBefore = new PostModel();
        if (before is { Status: true, Result: PostModel })
        {
            postBefore = (PostModel)before.Result;
        }

        var upvote = await _databasesManager.Upvote(postBefore, accessToken);

        var after = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfter = new PostModel();
        if (after is { Status: true, Result: PostModel })
        {
            postAfter = (PostModel)after.Result;
        }

        var resultStatus = upvote.Status;
        var counterBefore = postBefore.Counter;
        var counterAfter = postAfter.Counter;

        Assert.IsTrue(resultStatus && counterBefore == 0 && counterAfter == 1);
    }

    [Test]
    [Order(74)]
    public async Task Downvote_UserExistsPostExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T74_User";
        user.Password = "T74_Password";
        user.ConfirmPassword = "T74_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T74_Title";
        newPost.Content = "T74_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var before = await _databasesManager.GetPost((long)postId, accessToken);

        var postBefore = new PostModel();
        if (before is { Status: true, Result: PostModel })
        {
            postBefore = (PostModel)before.Result;
        }

        var downvote = await _databasesManager.Downvote(postBefore, accessToken);

        var after = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfter = new PostModel();
        if (after is { Status: true, Result: PostModel })
        {
            postAfter = (PostModel)after.Result;
        }

        var resultStatus = downvote.Status;
        var counterBefore = postBefore.Counter;
        var counterAfter = postAfter.Counter;

        Assert.IsTrue(resultStatus && counterBefore == 0 && counterAfter == -1);
    }

    [Test]
    [Order(75)]
    public async Task UpvoteOff_UserExistsPostExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T75_User";
        user.Password = "T75_Password";
        user.ConfirmPassword = "T75_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T75_Title";
        newPost.Content = "T75_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var before = await _databasesManager.GetPost((long)postId, accessToken);

        var postBefore = new PostModel();
        if (before is { Status: true, Result: PostModel })
        {
            postBefore = (PostModel)before.Result;
        }

        var upvote = await _databasesManager.Upvote(postBefore, accessToken);

        var afterUpvote = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfterUpvote = new PostModel();
        if (afterUpvote is { Status: true, Result: PostModel })
        {
            postAfterUpvote = (PostModel)afterUpvote.Result;
        }

        var upvoteOff = await _databasesManager.UpvoteOff(postAfterUpvote, accessToken);

        var afterOff = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfterOff = new PostModel();
        if (afterOff is { Status: true, Result: PostModel })
        {
            postAfterOff = (PostModel)afterOff.Result;
        }

        var resultStatus = upvoteOff.Status;
        var counterBefore = postBefore.Counter;
        var counterAfterUpvote = postAfterUpvote.Counter;
        var counterAfterOff = postAfterOff.Counter;

        Assert.IsTrue(resultStatus && counterBefore == 0 &&
                      counterAfterUpvote == 1 && counterAfterOff == 0);
    }

    [Test]
    [Order(76)]
    public async Task DownvoteOff_UserExistsPostExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T76_User";
        user.Password = "T76_Password";
        user.ConfirmPassword = "T76_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T76_Title";
        newPost.Content = "T76_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var before = await _databasesManager.GetPost((long)postId, accessToken);

        var postBefore = new PostModel();
        if (before is { Status: true, Result: PostModel })
        {
            postBefore = (PostModel)before.Result;
        }

        var downvote = await _databasesManager.Downvote(postBefore, accessToken);

        var afterDownvote = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfterDownvote = new PostModel();
        if (afterDownvote is { Status: true, Result: PostModel })
        {
            postAfterDownvote = (PostModel)afterDownvote.Result;
        }

        var downvoteOff = await _databasesManager.DownvoteOff(postAfterDownvote, accessToken);

        var afterOff = await _databasesManager.GetPost((long)postId, accessToken);
        var postAfterOff = new PostModel();
        if (afterOff is { Status: true, Result: PostModel })
        {
            postAfterOff = (PostModel)afterOff.Result;
        }

        var resultStatus = downvoteOff.Status;
        var counterBefore = postBefore.Counter;
        var counterAfterDownvote = postAfterDownvote.Counter;
        var counterAfterOff = postAfterOff.Counter;

        Assert.IsTrue(resultStatus && counterBefore == 0 &&
                      counterAfterDownvote == -1 && counterAfterOff == 0);
    }

    [Test]
    [Order(77)]
    public async Task DeletePost_ByAuthor_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T77_User";
        user.Password = "T77_Password";
        user.ConfirmPassword = "T77_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T77_Title";
        newPost.Content = "T77_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var getPost = await _databasesManager.GetPost((long)postId, accessToken);

        var post = new PostModel();
        if (getPost is { Status: true, Result: PostModel })
        {
            post = (PostModel)getPost.Result;
        }

        var result = await _databasesManager.DeletePost(post, accessToken);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(78)]
    public async Task DeletePost_byAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T78_User";
        user.Password = "T78_Password";
        user.ConfirmPassword = "T78_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T78_Title";
        newPost.Content = "T78_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T78_Admin";
        admin.Password = "T78_Password";
        admin.ConfirmPassword = "T78_Password";
        admin.TermsOfService = true;
        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var getPost = await _databasesManager.GetPost((long)postId, accessToken);

        var post = new PostModel();
        if (getPost is { Status: true, Result: PostModel })
        {
            post = (PostModel)getPost.Result;
        }

        var result = await _databasesManager.DeletePost(post, adminAccessToken);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(79)]
    public async Task DeletePost_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T79_User";
        user.Password = "T79_Password";
        user.ConfirmPassword = "T79_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T79_Title";
        newPost.Content = "T79_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T79_NotAdmin";
        notAdmin.Password = "T79_Password";
        notAdmin.ConfirmPassword = "T79_Password";
        notAdmin.TermsOfService = true;
        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var getPost = await _databasesManager.GetPost((long)postId, accessToken);

        var post = new PostModel();
        if (getPost is { Status: true, Result: PostModel })
        {
            post = (PostModel)getPost.Result;
        }

        var result = await _databasesManager.DeletePost(post, notAdminAccessToken);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(80)]
    public async Task CreateFact_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T80_User";
        user.Password = "T80_Password";
        user.ConfirmPassword = "T80_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T80_Title";
        newPost.Content = "T80_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T80_Admin";
        admin.Password = "T80_Password";
        admin.ConfirmPassword = "T80_Password";
        admin.TermsOfService = true;
        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager
            .CreateFact(adminAccessToken, (long)postId, "T80 Fact");

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(81)]
    public async Task CreateFact_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T81_User";
        user.Password = "T81_Password";
        user.ConfirmPassword = "T81_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T81_Title";
        newPost.Content = "T81_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T81_NotAdmin";
        notAdmin.Password = "T81_Password";
        notAdmin.ConfirmPassword = "T81_Password";
        notAdmin.TermsOfService = true;
        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager
            .CreateFact(notAdminAccessToken, (long)postId, "T81 Fact");

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(82)]
    public async Task GetFact_PostExistsFactExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T82_User";
        user.Password = "T82_Password";
        user.ConfirmPassword = "T82_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T82_Title";
        newPost.Content = "T82_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T82_Admin";
        admin.Password = "T82_Password";
        admin.ConfirmPassword = "T82_Password";
        admin.TermsOfService = true;
        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateFact(adminAccessToken, (long)postId, "T82 Fact");

        var getFact = await _databasesManager.GetFact((long)postId);

        var resultStatus = getFact.Status;
        var resultCorrectType = getFact.Result is string;
        var resultCorrectValue = resultCorrectType &&
                                 getFact.Result != null &&
                                 (string)getFact.Result == "T82 Fact";

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(83)]
    public async Task DeleteFact_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T83_User";
        user.Password = "T83_Password";
        user.ConfirmPassword = "T83_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T83_Title";
        newPost.Content = "T83_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T83_Admin";
        admin.Password = "T83_Password";
        admin.ConfirmPassword = "T83_Password";
        admin.TermsOfService = true;
        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateFact(adminAccessToken, (long)postId, "T83 Fact");

        var result = await _databasesManager.DeleteFact(adminAccessToken, (long)postId);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(84)]
    public async Task DeleteFact_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T84_User";
        user.Password = "T84_Password";
        user.ConfirmPassword = "T84_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T84_Title";
        newPost.Content = "T84_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;

        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T84_NotAdmin";
        notAdmin.Password = "T84_Password";
        notAdmin.ConfirmPassword = "T84_Password";
        notAdmin.TermsOfService = true;
        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var adminUserId = (ulong)register.Result!;
                await _databasesManager.Activate(adminUserId);
                var login = _databasesManager.Login(adminUserId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateFact(notAdminAccessToken, (long)postId, "T84 Fact");

        var result = await _databasesManager.DeleteFact(notAdminAccessToken, (long)postId);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(85)]
    public async Task GetLatestMessagesUsers_NoConversations_ReturnsEmptyList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T85_User";
        user.Password = "T85_Password";
        user.ConfirmPassword = "T85_Password";
        user.TermsOfService = true;

        string accessToken = "";

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var getLatestMessagesUsers = await _databasesManager
            .GetLatestMessagesUsers(accessToken);

        var resultStatus = getLatestMessagesUsers.Status;
        var resultCorrectType = getLatestMessagesUsers.Result is List<UserModel>;
        var resultCorrectValue = resultCorrectType &&
                                 getLatestMessagesUsers.Result != null &&
                                 ((List<UserModel>)getLatestMessagesUsers.Result).Count == 0;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(86)]
    public async Task GetLatestMessagesUsers_ConversationsExist_ReturnsUserModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T86_User1";
        user1.Password = "T86_Password";
        user1.ConfirmPassword = "T86_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T86_User2";
        user2.Password = "T86_Password";
        user2.ConfirmPassword = "T86_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        _databasesManager.SendMessage(userId1, userId2, "T86 Message");

        var getLatestMessagesUsers = await _databasesManager
            .GetLatestMessagesUsers(accessToken1);

        var resultStatus = getLatestMessagesUsers.Status;
        var resultCorrectType = getLatestMessagesUsers.Result is List<UserModel>;
        var resultCorrectValue = resultCorrectType &&
                                 getLatestMessagesUsers.Result != null &&
                                 ((List<UserModel>)getLatestMessagesUsers.Result).Count == 1;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(87)]
    public async Task GetMessages_ConversationExists_ReturnsMessageModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T87_User1";
        user1.Password = "T87_Password";
        user1.ConfirmPassword = "T87_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T87_User2";
        user2.Password = "T87_Password";
        user2.ConfirmPassword = "T87_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        _databasesManager.SendMessage(userId1, userId2, "T87 Message1");
        _databasesManager.SendMessage(userId2, userId1, "T87 Message2");

        var getMessages = _databasesManager.GetMessages(userId1, userId2);

        var resultStatus = getMessages.Status;
        var resultCorrectType = getMessages.Result is List<MessageModel>;
        var resultCorrectValue = resultCorrectType &&
                                 getMessages.Result != null &&
                                 ((List<MessageModel>)getMessages.Result).Count == 2;

        Assert.IsTrue(resultStatus && resultCorrectType && resultCorrectValue);
    }

    [Test]
    [Order(88)]
    public async Task SendMessage_UsersExist_ReturnsMessageId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T88_User1";
        user1.Password = "T88_Password";
        user1.ConfirmPassword = "T88_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T88_User2";
        user2.Password = "T88_Password";
        user2.ConfirmPassword = "T88_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var result = _databasesManager.SendMessage(userId1, userId2, "T88 Message");

        Assert.IsTrue(result is { Status: true, Result: ulong });
    }

    [Test]
    [Order(89)]
    public async Task GetMessage_BySender_ReturnsMessageModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T89_User1";
        user1.Password = "T89_Password";
        user1.ConfirmPassword = "T89_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T89_User2";
        user2.Password = "T89_Password";
        user2.ConfirmPassword = "T89_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T89 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var result = await _databasesManager.GetMessage(accessToken1, messageId);

        Assert.IsTrue(result is { Status: true, Result: MessageModel });
    }

    [Test]
    [Order(90)]
    public async Task GetMessage_ByReceiver_ReturnsMessageModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T90_User1";
        user1.Password = "T90_Password";
        user1.ConfirmPassword = "T90_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T90_User2";
        user2.Password = "T90_Password";
        user2.ConfirmPassword = "T90_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T90 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var result = await _databasesManager.GetMessage(accessToken2, messageId);

        Assert.IsTrue(result is { Status: true, Result: MessageModel });
    }

    [Test]
    [Order(91)]
    public async Task GetMessage_ByAdmin_ReturnsMessageModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T91_User1";
        user1.Password = "T91_Password";
        user1.ConfirmPassword = "T91_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T91_User2";
        user2.Password = "T91_Password";
        user2.ConfirmPassword = "T91_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T91 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T91_Admin";
        admin.Password = "T91_Password";
        admin.ConfirmPassword = "T91_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.GetMessage(adminAccessToken, messageId);

        Assert.IsTrue(result is { Status: true, Result: MessageModel });
    }

    [Test]
    [Order(92)]
    public async Task GetMessage_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T92_User1";
        user1.Password = "T92_Password";
        user1.ConfirmPassword = "T92_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T92_User2";
        user2.Password = "T92_Password";
        user2.ConfirmPassword = "T92_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T92 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T92_NotAdmin";
        notAdmin.Password = "T92_Password";
        notAdmin.ConfirmPassword = "T92_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var result = await _databasesManager.GetMessage(notAdminAccessToken, messageId);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(93)]
    public async Task DeleteMessage_BySender_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T93_User1";
        user1.Password = "T93_Password";
        user1.ConfirmPassword = "T93_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T93_User2";
        user2.Password = "T93_Password";
        user2.ConfirmPassword = "T93_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T93 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var getMessage = await _databasesManager.GetMessage(accessToken1, messageId);

        var message = new MessageModel();
        if (getMessage.Status && getMessage.Result is MessageModel)
        {
            message = (MessageModel)getMessage.Result;
        }

        var result = await _databasesManager.DeleteMessage(message, accessToken1);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(94)]
    public async Task DeleteMessage_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T94_User1";
        user1.Password = "T94_Password";
        user1.ConfirmPassword = "T94_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T94_User2";
        user2.Password = "T94_Password";
        user2.ConfirmPassword = "T94_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T94 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T94_Admin";
        admin.Password = "T94_Password";
        admin.ConfirmPassword = "T94_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var getMessage = await _databasesManager.GetMessage(adminAccessToken, messageId);

        var message = new MessageModel();
        if (getMessage.Status && getMessage.Result is MessageModel)
        {
            message = (MessageModel)getMessage.Result;
        }

        var result = await _databasesManager.DeleteMessage(message, adminAccessToken);

        Assert.IsTrue(result.Status);
    }

    [Test]
    [Order(95)]
    public async Task DeleteMessage_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T95_User1";
        user1.Password = "T95_Password";
        user1.ConfirmPassword = "T95_Password";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T95_User2";
        user2.Password = "T95_Password";
        user2.ConfirmPassword = "T95_Password";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T95 Message");

        ulong messageId = 9999;
        if (sendMessage.Status && sendMessage.Result is ulong)
        {
            messageId = (ulong)sendMessage.Result;
        }

        var getMessage = await _databasesManager.GetMessage(accessToken2, messageId);

        var message = new MessageModel();
        if (getMessage.Status && getMessage.Result is MessageModel)
        {
            message = (MessageModel)getMessage.Result;
        }

        var result = await _databasesManager.DeleteMessage(message, accessToken2);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(96)]
    public async Task CreateComment_ActiveUser_ReturnsCommentId()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T96_User";
        user.Password = "T96_Password";
        user.ConfirmPassword = "T96_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T96_Title";
        newPost.Content = "T96_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T96_Comment";
        newComment.AuthorId = (long)userId;

        var result = await _databasesManager.CreateComment(accessToken, newComment, (long)postId);

        Assert.IsTrue(result is { Status: true, Result: ulong });
    }

    [Test]
    [Order(97)]
    public async Task CreateComment_BannedUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T97_User";
        user.Password = "T97_Password";
        user.ConfirmPassword = "T97_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T97_Title";
        newPost.Content = "T97_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T97_Admin";
        admin.Password = "T97_Password";
        admin.ConfirmPassword = "T97_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.BanUser(adminAccessToken, user.Username, "T97 Reason", 1);

        var newComment = new CommentModel();
        newComment.Content = "T97_Comment";
        newComment.AuthorId = (long)userId;

        var result = await _databasesManager.CreateComment(accessToken, newComment, (long)postId);

        Assert.IsFalse(result.Status);
    }

    [Test]
    [Order(98)]
    public async Task GetComments_CommentsExist_ReturnsCommentModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T98_User";
        user.Password = "T98_Password";
        user.ConfirmPassword = "T98_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T98_Title";
        newPost.Content = "T98_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment1 = new CommentModel();
        newComment1.Content = "T98_Comment";
        newComment1.AuthorId = (long)userId;

        var newComment2 = new CommentModel();
        newComment2.Content = "T98_Comment";
        newComment2.AuthorId = (long)userId;

        await _databasesManager.CreateComment(accessToken, newComment1, (long)postId);
        await _databasesManager.CreateComment(accessToken, newComment2, (long)postId);

        var getComments = await _databasesManager.GetComments((long)postId);

        Assert.IsTrue(getComments is { Status: true, Result: List<CommentModel> { Count: 2 } });
    }

    [Test]
    [Order(99)]
    public async Task GetComments_NoComments_ReturnsEmptyList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T99_User";
        user.Password = "T99_Password";
        user.ConfirmPassword = "T99_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T99_Title";
        newPost.Content = "T99_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var getComments = await _databasesManager.GetComments((long)postId);

        Assert.IsTrue(getComments is { Status: true, Result: List<CommentModel> { Count: 0 } });
    }

    [Test]
    [Order(100)]
    public async Task GetComment_CommentExists_ReturnsCommentModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T100_User";
        user.Password = "T100_Password";
        user.ConfirmPassword = "T100_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T100_Title";
        newPost.Content = "T100_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T100_Comment";
        newComment.AuthorId = (long)userId;

        var createComment = await _databasesManager
            .CreateComment(accessToken, newComment, (long)postId);

        ulong commentId = 9999;
        if (createComment is { Status: true, Result: ulong })
        {
            commentId = (ulong)createComment.Result;
        }

        var getComment = await _databasesManager.GetComment((long)commentId);

        Assert.IsTrue(getComment is { Status: true, Result: CommentModel });
    }

    [Test]
    [Order(101)]
    public async Task DeleteComment_ByAuthor_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T101_User";
        user.Password = "T101_Password";
        user.ConfirmPassword = "T101_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T101_Title";
        newPost.Content = "T101_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T101_Comment";
        newComment.AuthorId = (long)userId;

        var createComment = await _databasesManager
            .CreateComment(accessToken, newComment, (long)postId);

        ulong commentId = 9999;
        if (createComment is { Status: true, Result: ulong })
        {
            commentId = (ulong)createComment.Result;
        }

        var getComment = await _databasesManager.GetComment((long)commentId);

        var comment = new CommentModel();
        if (getComment is { Status: true, Result: CommentModel })
        {
            comment = (CommentModel)getComment.Result;
        }

        var deleteComment = await _databasesManager.DeleteComment(comment, accessToken);

        Assert.IsTrue(deleteComment.Status);
    }

    [Test]
    [Order(102)]
    public async Task DeleteComment_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T102_User";
        user.Password = "T102_Password";
        user.ConfirmPassword = "T102_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T102_Title";
        newPost.Content = "T102_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T102_Comment";
        newComment.AuthorId = (long)userId;

        var createComment = await _databasesManager
            .CreateComment(accessToken, newComment, (long)postId);

        ulong commentId = 9999;
        if (createComment is { Status: true, Result: ulong })
        {
            commentId = (ulong)createComment.Result;
        }

        var getComment = await _databasesManager.GetComment((long)commentId);

        var comment = new CommentModel();
        if (getComment is { Status: true, Result: CommentModel })
        {
            comment = (CommentModel)getComment.Result;
        }

        var admin = new RegistrationModel();
        admin.Username = "T102_Admin";
        admin.Password = "T102_Password";
        admin.ConfirmPassword = "T102_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var deleteComment = await _databasesManager.DeleteComment(comment, adminAccessToken);

        Assert.IsTrue(deleteComment.Status);
    }

    [Test]
    [Order(103)]
    public async Task DeleteComment_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T103_User";
        user.Password = "T103_Password";
        user.ConfirmPassword = "T103_Password";
        user.TermsOfService = true;

        string accessToken = "";
        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T103_Title";
        newPost.Content = "T103_Content";
        var post = await _databasesManager
            .CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (post is { Status: true, Result: ulong })
        {
            postId = (ulong)post.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T103_Comment";
        newComment.AuthorId = (long)userId;

        var createComment = await _databasesManager
            .CreateComment(accessToken, newComment, (long)postId);

        ulong commentId = 9999;
        if (createComment is { Status: true, Result: ulong })
        {
            commentId = (ulong)createComment.Result;
        }

        var getComment = await _databasesManager.GetComment((long)commentId);

        var comment = new CommentModel();
        if (getComment is { Status: true, Result: CommentModel })
        {
            comment = (CommentModel)getComment.Result;
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T103_NotAdmin";
        notAdmin.Password = "T103_Password";
        notAdmin.ConfirmPassword = "T103_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var deleteComment = await _databasesManager.DeleteComment(comment, notAdminAccessToken);

        Assert.IsFalse(deleteComment.Status);
    }

    [Test]
    [Order(104)]
    public async Task CreateReport_ByBannedUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T104_User1";
        user1.Password = "T104_Password1";
        user1.ConfirmPassword = "T104_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T104_Title";
        newPost.Content = "T104_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T104_User2";
        user2.Password = "T104_Password2";
        user2.ConfirmPassword = "T104_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T104_Admin";
        admin.Password = "T104_Password";
        admin.ConfirmPassword = "T104_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.BanUser(adminAccessToken, "T104_User2", "T104 Reason", 1);

        var report = await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)postId,
            Content.Post,
            accessToken2,
            "T104 Report reason");

        Assert.IsFalse(report.Status);
    }

    [Test]
    [Order(105)]
    public async Task CreateReport_PostDisinformation_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T105_User1";
        user1.Password = "T105_Password1";
        user1.ConfirmPassword = "T105_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T105_Title";
        newPost.Content = "T105_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T105_User2";
        user2.Password = "T105_Password2";
        user2.ConfirmPassword = "T105_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var report = await _databasesManager.CreateReport(
            ReportType.FactChecker,
            (long)postId,
            Content.Post,
            accessToken2,
            "T105 Report reason");

        Assert.IsTrue(report.Status);
    }

    [Test]
    [Order(106)]
    public async Task CreateReport_Post_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T106_User1";
        user1.Password = "T106_Password1";
        user1.ConfirmPassword = "T106_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T106_Title";
        newPost.Content = "T106_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T106_User2";
        user2.Password = "T106_Password2";
        user2.ConfirmPassword = "T106_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var report = await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)postId,
            Content.Post,
            accessToken2,
            "T106 Report reason");

        Assert.IsTrue(report.Status);
    }

    [Test]
    [Order(107)]
    public async Task CreateReport_Comment_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T107_User1";
        user1.Password = "T107_Password1";
        user1.ConfirmPassword = "T107_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T107_Title";
        newPost.Content = "T107_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var newComment = new CommentModel();
        newComment.Content = "T107_Comment";
        newComment.AuthorId = (long)userId1;

        var createComment = await _databasesManager
            .CreateComment(accessToken1, newComment, (long)postId);

        ulong commentId = 9999;
        if (createComment is { Status: true, Result: ulong })
        {
            commentId = (ulong)createComment.Result;
        }


        var user2 = new RegistrationModel();
        user2.Username = "T107_User2";
        user2.Password = "T107_Password2";
        user2.ConfirmPassword = "T107_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var report = await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)commentId,
            Content.Comment,
            accessToken2,
            "T107 Report reason");

        Assert.IsTrue(report.Status);
    }

    [Test]
    [Order(108)]
    public async Task CreateReport_Message_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T108_User1";
        user1.Password = "T108_Password1";
        user1.ConfirmPassword = "T108_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";
        ulong userId1 = 9999;

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                userId1 = (ulong)register.Result!;
                await _databasesManager.Activate(userId1);
                var login = _databasesManager.Login(userId1);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var user2 = new RegistrationModel();
        user2.Username = "T108_User2";
        user2.Password = "T108_Password2";
        user2.ConfirmPassword = "T108_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";
        ulong userId2 = 9999;

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                userId2 = (ulong)register.Result!;
                await _databasesManager.Activate(userId2);
                var login = _databasesManager.Login(userId2);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        var sendMessage = _databasesManager.SendMessage(userId1, userId2, "T108_Message");

        ulong messageId = 9999;
        if (sendMessage is { Status: true, Result: ulong })
        {
            messageId = (ulong)sendMessage.Result;
        }

        var report = await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)messageId,
            Content.Message,
            accessToken2,
            "T108 Report reason");

        Assert.IsTrue(report.Status);
    }

    [Test]
    [Order(109)]
    public async Task CreateReport_AutoReport_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T109_User";
        user1.Password = "T109_Password";
        user1.ConfirmPassword = "T109_Password";
        user1.TermsOfService = true;

        string accessToken = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T109_Title";
        newPost.Content = "T109_Content";

        var createPost = await _databasesManager.CreatePost(accessToken, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var report = await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)postId,
            Content.Post,
            "",
            "T109 Report reason",
            true);

        Assert.IsTrue(report.Status);
    }

    [Test]
    [Order(110)]
    public async Task BanUser_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T110_User";
        user.Password = "T110_Password";
        user.ConfirmPassword = "T110_Password";
        user.TermsOfService = true;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T110_Admin";
        admin.Password = "T110_Password";
        admin.ConfirmPassword = "T110_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var ban = await _databasesManager
            .BanUser(adminAccessToken, "T110_User", "T110_Reason", 1);

        Assert.IsTrue(ban.Status);
    }

    [Test]
    [Order(111)]
    public async Task BanUser_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T111_User";
        user.Password = "T111_Password";
        user.ConfirmPassword = "T111_Password";
        user.TermsOfService = true;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T111_NotAdmin";
        notAdmin.Password = "T111_Password";
        notAdmin.ConfirmPassword = "T111_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var ban = await _databasesManager
            .BanUser(notAdminAccessToken, "T111_User", "T111_Reason", 1);

        Assert.IsFalse(ban.Status);
    }

    [Test]
    [Order(112)]
    public async Task UnBanUser_ByAdmin_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T112_User";
        user.Password = "T112_Password";
        user.ConfirmPassword = "T112_Password";
        user.TermsOfService = true;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T112_Admin";
        admin.Password = "T112_Password";
        admin.ConfirmPassword = "T112_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager
            .BanUser(adminAccessToken, "T112_User", "T112_Reason", 1);

        var unBan = await _databasesManager.UnBanUser(adminAccessToken, "T112_User");

        Assert.IsTrue(unBan.Status);
    }

    [Test]
    [Order(113)]
    public async Task UnBanUser_ByUser_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T113_User";
        user.Password = "T113_Password";
        user.ConfirmPassword = "T113_Password";
        user.TermsOfService = true;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T113_Admin";
        admin.Password = "T113_Password";
        admin.ConfirmPassword = "T113_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager
            .BanUser(adminAccessToken, "T113_User", "T113_Reason", 1);

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T113_NotAdmin";
        notAdmin.Password = "T113_Password";
        notAdmin.ConfirmPassword = "T113_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var unBan = await _databasesManager.UnBanUser(notAdminAccessToken, "T113_User");

        Assert.IsFalse(unBan.Status);
    }

    [Test]
    [Order(114)]
    public async Task GetReport_FactCheckerReport_ReturnsReportModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T114_User1";
        user1.Password = "T114_Password1";
        user1.ConfirmPassword = "T114_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T114_Title";
        newPost.Content = "T114_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T114_User2";
        user2.Password = "T114_Password2";
        user2.ConfirmPassword = "T114_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateReport(
            ReportType.FactChecker,
            (long)postId,
            Content.Post,
            accessToken2,
            "T114 Report reason");

        var admin = new RegistrationModel();
        admin.Username = "T114_Admin";
        admin.Password = "T114_Password";
        admin.ConfirmPassword = "T114_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var getFactCheckerReport = await _databasesManager
            .GetReport(adminAccessToken, ReportType.FactChecker);

        Assert.IsTrue(getFactCheckerReport is { Status: true, Result: ReportModel });
    }

    [Test]
    [Order(115)]
    public async Task GetReport_ModeratorReport_ReturnsReportModel()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T115_User1";
        user1.Password = "T115_Password1";
        user1.ConfirmPassword = "T115_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T115_Title";
        newPost.Content = "T115_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T115_User2";
        user2.Password = "T115_Password2";
        user2.ConfirmPassword = "T115_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateReport(
            ReportType.Moderator,
            (long)postId,
            Content.Post,
            accessToken2,
            "T115 Report reason");

        var admin = new RegistrationModel();
        admin.Username = "T115_Admin";
        admin.Password = "T115_Password";
        admin.ConfirmPassword = "T115_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var getModeratorReport = await _databasesManager
            .GetReport(adminAccessToken, ReportType.Moderator);

        Assert.IsTrue(getModeratorReport is { Status: true, Result: ReportModel });
    }

    [Test]
    [Order(116)]
    public async Task GetReport_User_Failure()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T116_User1";
        user1.Password = "T116_Password1";
        user1.ConfirmPassword = "T116_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T116_Title";
        newPost.Content = "T116_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T116_User2";
        user2.Password = "T116_Password2";
        user2.ConfirmPassword = "T116_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateReport(
            ReportType.FactChecker,
            (long)postId,
            Content.Post,
            accessToken2,
            "T116 Report reason");

        var notAdmin = new RegistrationModel();
        notAdmin.Username = "T116_NotAdmin";
        notAdmin.Password = "T116_Password";
        notAdmin.ConfirmPassword = "T116_Password";
        notAdmin.TermsOfService = true;

        string notAdminAccessToken = "";

        if (notAdmin.IsValid)
        {
            var register = _databasesManager.Register(notAdmin);
            if (register.Status)
            {
                var notAdminId = (ulong)register.Result!;
                await _databasesManager.Activate(notAdminId);
                var login = _databasesManager.Login(notAdminId);
                if (login.Status && login.Result is string)
                {
                    notAdminAccessToken = (string)login.Result;
                }
            }
        }

        var getFactCheckerReport = await _databasesManager
            .GetReport(notAdminAccessToken, ReportType.FactChecker);

        Assert.IsFalse(getFactCheckerReport.Status);
    }

    [Test]
    [Order(117)]
    public async Task DeleteReport_ReportExists_Success()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user1 = new RegistrationModel();
        user1.Username = "T117_User1";
        user1.Password = "T117_Password1";
        user1.ConfirmPassword = "T117_Password1";
        user1.TermsOfService = true;

        string accessToken1 = "";

        if (user1.IsValid)
        {
            var register = _databasesManager.Register(user1);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken1 = (string)login.Result;
                }
            }
        }

        var newPost = new PostModel();
        newPost.Title = "T117_Title";
        newPost.Content = "T117_Content";

        var createPost = await _databasesManager.CreatePost(accessToken1, newPost, new List<string>(), null);

        ulong postId = 9999;
        if (createPost is { Status: true, Result: ulong })
        {
            postId = (ulong)createPost.Result;
        }

        var user2 = new RegistrationModel();
        user2.Username = "T117_User2";
        user2.Password = "T117_Password2";
        user2.ConfirmPassword = "T117_Password2";
        user2.TermsOfService = true;

        string accessToken2 = "";

        if (user2.IsValid)
        {
            var register = _databasesManager.Register(user2);
            if (register.Status)
            {
                var userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
                var login = _databasesManager.Login(userId);
                if (login.Status && login.Result is string)
                {
                    accessToken2 = (string)login.Result;
                }
            }
        }

        await _databasesManager.CreateReport(
            ReportType.FactChecker,
            (long)postId,
            Content.Post,
            accessToken2,
            "T117 Report reason");

        var admin = new RegistrationModel();
        admin.Username = "T117_Admin";
        admin.Password = "T117_Password";
        admin.ConfirmPassword = "T117_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        var getFactCheckerReport = await _databasesManager
            .GetReport(adminAccessToken, ReportType.FactChecker);

        ulong reportId = 9999;
        if (getFactCheckerReport is { Status: true, Result: ReportModel })
        {
            reportId = ((ReportModel)getFactCheckerReport.Result).ReportId;
        }

        var deleteReport = await _databasesManager
            .DeleteReport(adminAccessToken, reportId);

        Assert.IsTrue(deleteReport.Status);
    }

    [Test]
    [Order(118)]
    public async Task GetBanHistory_UserExists_ReturnsBanModelList()
    {
        if (_connected == false || _mySqlInit == false || _neo4jInit == false)
        {
            Assert.Fail("Databases connection or initialization failed.");
            return;
        }

        var user = new RegistrationModel();
        user.Username = "T118_User";
        user.Password = "T118_Password";
        user.ConfirmPassword = "T118_Password";
        user.TermsOfService = true;

        ulong userId = 9999;

        if (user.IsValid)
        {
            var register = _databasesManager.Register(user);
            if (register.Status)
            {
                userId = (ulong)register.Result!;
                await _databasesManager.Activate(userId);
            }
        }

        var admin = new RegistrationModel();
        admin.Username = "T118_Admin";
        admin.Password = "T118_Password";
        admin.ConfirmPassword = "T118_Password";
        admin.TermsOfService = true;

        string adminAccessToken = "";

        if (admin.IsValid)
        {
            var register = _databasesManager.Register(admin, true);
            if (register.Status)
            {
                var adminId = (ulong)register.Result!;
                await _databasesManager.Activate(adminId);
                var login = _databasesManager.Login(adminId);
                if (login.Status && login.Result is string)
                {
                    adminAccessToken = (string)login.Result;
                }
            }
        }

        await _databasesManager.BanUser(adminAccessToken, "T118_User", "T118 Ban1", 1);
        await _databasesManager.BanUser(adminAccessToken, "T118_User", "T118 Ban2", 2);
        await _databasesManager.BanUser(adminAccessToken, "T118_User", "T118 Ban3", 3);
        await _databasesManager.BanUser(adminAccessToken, "T118_User", "T118 Ban4", 4);

        var getUser = await _databasesManager.GetUser(userId);

        var userModel = new UserModel();
        if (getUser is { Status: true, Result: UserModel })
        {
            userModel = (UserModel)getUser.Result;
        }

        var getBanHistory = await _databasesManager.GetBanHistory(userModel);

        Assert.IsTrue(getBanHistory is { Status: true, Result: List<BanModel> { Count: 4 } });
    }
}
