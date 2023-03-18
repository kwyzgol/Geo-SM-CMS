using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class MessageModel : DatabaseConnected
{
    public ulong MessageId { get; set; } = 0;
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_message_is_required_")]
    [StringLength(maximumLength: 1000,
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "The_message_must_be_a_maximum_of_1000_characters_")]
    public string Content { get; set; } = "";
    public ulong SenderId { get; set; } = 0;
    public ulong ReceiverId { get; set; } = 0;

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }

    public static async Task<OperationResult> GetUsersLatest(string? accessToken, DatabasesManager? databases = null)
    {
        if(databases == null) databases = DatabasesBase;

        return accessToken != null ? await databases.GetLatestMessagesUsers(accessToken) : 
            new OperationResult(false, "Error");
    }

    public static OperationResult Get(ulong? user1, ulong? user2, ulong? maxId, DatabasesManager? databases = null)
    {
        if(databases == null) databases = DatabasesBase;

        return user1 != null && user2 != null ? databases.GetMessages((ulong)user1, (ulong)user2, maxId) : 
            new OperationResult(false, "Error");
    }

    public static OperationResult Send(ulong sender, ulong receiver, string message, DatabasesManager? databases = null)
    {
        if(databases == null) databases = DatabasesBase;

        return databases.SendMessage(sender, receiver, message);
    }

    public static async Task<OperationResult> Get(ulong? messageId, string? accessToken, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;
        
        return accessToken != null && messageId != null ? await databases.GetMessage(accessToken, (ulong)messageId) : 
            new OperationResult(false, "Error");
    }

    public async Task<OperationResult> Delete(string? accessToken)
    {
        return accessToken != null ? await Databases.DeleteMessage(this, accessToken) : 
            new OperationResult(false, "Error");
    }
}