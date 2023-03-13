namespace WebApp.Data;

public class PlaceModel : DatabaseConnected
{
    public double Latitude { get; set; } = 0;
    public double Longitude { get; set; } = 0;
    public string Description { get; set; } = "No description";

    public PlaceModel() { }

    public PlaceModel(double latitude, double longitude, string description)
    {
        Latitude = latitude;
        Longitude = longitude;
        Description = description;
    }

    public static async Task<OperationResult> SearchPlace(string? place, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return place != null ? await databases.SearchPlace(place) : new OperationResult(false, "Error");
    }
}

