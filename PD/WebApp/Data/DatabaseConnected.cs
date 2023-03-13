namespace WebApp.Data;

public abstract class DatabaseConnected
{
    public static DatabasesManager DatabasesBase { get; set; }
        = new DatabasesManager("", "", "", "", "", "", "");

    public DatabasesManager Databases { get; set; } = DatabasesBase;
}
