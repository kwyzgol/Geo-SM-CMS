using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class CommentModel : DatabaseConnected
{
    public long CommentId { get; set; } = -1;
    public long AuthorId { get; set; } = -1;
    public string Author { get; set; } = "";
    public string Avatar { get; set; } = "core/user.png";
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Required(ErrorMessage = "The comment is required.")]
    [StringLength(maximumLength: 2000,
        ErrorMessage = "The comment must be a maximum of 2000 characters.")]
    public string Content { get; set; } = "";

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }

    public static async Task<OperationResult> Create(string? accessToken, CommentModel? comment, long? postId, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return comment != null && accessToken != null && postId != null ? 
            await databases.CreateComment((string)accessToken, (CommentModel)comment, (long)postId) : 
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> GetMultiple(long? postId, long? minId = 0, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        if(minId == null) minId = 0;

        return postId != null ? await databases.GetComments((long)postId, (long)minId) : 
            new OperationResult(false, "Error");
    }
    
    public static async Task<OperationResult> Get(long? commentId, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return commentId != null ? await databases.GetComment((long)commentId) :
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> Delete(string? accessToken)
    {
        return accessToken != null ? await Databases.DeleteComment(this, accessToken) :
            new OperationResult(false, "Error");
    }
}
