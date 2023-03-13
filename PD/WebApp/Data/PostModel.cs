namespace WebApp.Data;

public class PostModel : DatabaseConnected
{
    public long PostId { get; set; } = 0;
    public string Author { get; set; } = "";
    public long AuthorId { get; set; } = 0;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Img { get; set; } = "";
    public int Counter { get; set; } = 0;
    public string Fact { get; set; } = "";
    public bool SelectedUp { get; set; } = false;
    public bool SelectedDown { get; set; } = false;
    public bool BlockSearchEngines { get; set; } = false;

    public static async Task<OperationResult> Create(string? accessToken, PostModel? post, List<string>? tags, GeoPointModel? geoPoint, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null 
               && post != null 
               && tags != null ? 
            await databases.CreatePost(accessToken, post, tags, geoPoint) : 
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Get(long? postId, string? accessToken,  DatabasesManager? databases = null)
    {
        if(databases == null) databases = DatabasesBase;

        return postId != null ? await databases.GetPost((long)postId, accessToken) : 
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> GetMultiple(ViewMode view, List<string>? tags, GeoPointModel? geoPoint, List<long>? alreadyLoaded, string? username, string? accessToken, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return await databases.GetPosts(view, tags, geoPoint, alreadyLoaded, username, accessToken);
    }

    public static async Task<OperationResult> GetTags(DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return await databases.GetTags();
    }

    public async Task<OperationResult> Upvote(string? accessToken)
    {
        return accessToken != null ? await Databases.Upvote(this, accessToken) : 
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> Downvote(string? accessToken)
    {
        return accessToken != null ? await Databases.Downvote(this, accessToken) :
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> UpvoteOff(string? accessToken)
    {
        return accessToken != null ? await Databases.UpvoteOff(this, accessToken) :
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> DownvoteOff(string? accessToken)
    {
        return accessToken != null ? await Databases.DownvoteOff(this, accessToken) :
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> Delete(string? accessToken)
    {
        return accessToken != null ? await Databases.DeletePost(this, accessToken) : 
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> GetFact(long? postId, DatabasesManager? databases = null)
    {     
          if (databases == null) databases = DatabasesBase;

          return postId != null ? await databases.GetFact((long)postId) : 
              new OperationResult(false, "Error");
    }     
          
    public static async Task<OperationResult> CreateFact(string? accessToken, long? postId, string? content, 
        DatabasesManager? databases = null)
    {     
          if (databases == null) databases = DatabasesBase;

          return accessToken != null && postId != null && content != null ? 
              await databases.CreateFact(accessToken, (long)postId, content) : 
              new OperationResult(false, "Error");
    }     
          
    public static async Task<OperationResult> DeleteFact(string? accessToken, long? postId, 
        DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && postId != null ? 
            await databases.DeleteFact(accessToken, (long)postId) : 
            new OperationResult(false, "Error");
    }
}