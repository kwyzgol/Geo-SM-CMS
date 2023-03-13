using System.Runtime.CompilerServices;

namespace WebApp.Cms;

public static class Cms
{
    public static GeneralSettings? System { get; set; }
    public static LanguageManager? Language { get; set; } = new LanguageManager();
    public static AuthManager? Auth { get; set; }

    public static DatabasesManager? Databases { get; set; }

    public static ReCaptchaManager? ReCaptcha { get; set; }

    public static bool CreateAdmin { get; set; } = false;

    public static async Task<bool> Init()
    {
        if (ImgDataInit()) Console.WriteLine("Images data initialized.");
        else
        {
            Console.WriteLine("Images data not initialized.");
            return false;
        }

        if (await DatabasesConnect())
        {
            Console.WriteLine("Connected to the databases successfuly.");
            Databases?.SetAsGlobal();
        }
        else
        {
            Console.WriteLine("Failed to connect to the databases.");
            return false;
        }

        OperationResult databasesInitResult = await DatabasesInit();
        if (databasesInitResult.Status) Console.WriteLine("Databases initialized.");
        else
        {
            if (databasesInitResult.Message.Equals("Error"))
            {
                Console.WriteLine("Error occurred during a database initialization.");
                return false;
            }
        }

        if (GetSettingsData()) Console.WriteLine("CMS settings loaded.");
        else
        {
            Console.WriteLine("Couldn't load CMS settings");
            return false;
        }

        if (AdminNotExist()) CreateAdmin = true;

        Console.WriteLine();
        return true;
    }

    private static bool ImgDataInit()
    {
        try
        {
            if (!Directory.Exists("./wwwroot/img/core"))
            {
                DirectoryInfo imgRawDir = new DirectoryInfo("./wwwroot/img_raw");
                DirectoryInfo[] subDirs = imgRawDir.GetDirectories();
                Directory.CreateDirectory("./wwwroot/img");

                foreach (FileInfo file in imgRawDir.GetFiles())
                {
                    string target = Path.Combine("./wwwroot/img", file.Name);
                    file.CopyTo(target);
                }

                foreach (DirectoryInfo subDir in subDirs)
                {
                    Directory.CreateDirectory($"./wwwroot/img/{subDir.Name}");
                    foreach (FileInfo file in subDir.GetFiles())
                    {
                        string target = Path.Combine($"./wwwroot/img/{subDir.Name}", file.Name);
                        file.CopyTo(target);
                    }
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    private static async Task<bool> DatabasesConnect()
    {
        string? mysqlHostname = Environment.GetEnvironmentVariable("MYSQL_HOSTNAME");
        if (mysqlHostname == null) mysqlHostname = "pd-mysql";

        string? mysqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
        if (mysqlDatabase == null) mysqlDatabase = "db";

        string? mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");
        if (mysqlUser == null) mysqlUser = "root";

        string? mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
        if (mysqlPassword == null)
        {
            Console.WriteLine("MYSQL_PASSWORD variable not provided.");
            return false;
        }

        string? neo4jHostname = Environment.GetEnvironmentVariable("NEO4J_HOSTNAME");
        if (neo4jHostname == null) neo4jHostname = "pd-neo4j";

        string? neo4jUser = Environment.GetEnvironmentVariable("NEO4J_USER");
        if (neo4jUser == null) neo4jUser = "neo4j";

        string? neo4jPassword = Environment.GetEnvironmentVariable("NEO4J_PASSWORD");
        if (neo4jPassword == null)
        {
            Console.WriteLine("NEO4J_PASSWORD not provided.");
            return false;
        }

        Databases = new DatabasesManager(
            mysqlHostname,
            mysqlDatabase,
            mysqlUser,
            mysqlPassword,
            neo4jHostname,
            neo4jUser,
            neo4jPassword);

        OperationResult connectionTest = await Databases.TestConnection();
        return connectionTest.Status;
    }

    private static async Task<OperationResult> DatabasesInit()
    {
        if (Databases != null && !(Databases.CheckTableExist().Status))
        {
            var mySqlInit = Databases.MySqlInit();
            var neo4jInit = await Databases.Neo4jInit();

            if (mySqlInit.Status && neo4jInit.Status)
            {
                return Databases.MySqlInsert();
            }
            else
            {
                return new OperationResult(
                    mySqlInit.Status && neo4jInit.Status,
                    mySqlInit.Message.Equals("Error") ||
                    neo4jInit.Message.Equals("Error") ? "Error" : "");
            }
        }

        return new OperationResult(false);
    }

    private static bool GetSettingsData()
    {
        System = new GeneralSettings();
        Auth = new AuthManager();
        ReCaptcha = new ReCaptchaManager();
        return Databases?.GetSettings(System, Auth, ReCaptcha).Status ?? false;
    }

    private static bool AdminNotExist()
    {
        if (Databases != null)
        {
            OperationResult adminExist = Databases.CheckAdminExists();
            return adminExist.Status == false &&
                   adminExist.Message.Equals("Error") == false;

        }
        else return false;
    }
}