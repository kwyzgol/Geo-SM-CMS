using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class UserAccessModel : DatabaseConnected
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_username_is_required_")]
    [StringLength(maximumLength: 20,
        MinimumLength = 4,
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_username_must_be_between_4_and_20_characters_long_")]
    [RegularExpression("^[A-Za-z0-9_.]{4,20}$",
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Available_characters_for_the_username_")]
    public string? Username { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_password_is_required_")]
    [StringLength(64, MinimumLength = 8, 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_password_must_be_between_8_and_64_characters_long_")]
    [RegularExpression("^[A-Za-z0-9!@$%^&*<>_.,?-]{8,64}$",
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "Available_characters_for_the_password_")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public virtual bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }

    public static OperationResult ChangePassword(ulong? id, string? password, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null && password != null
            ? databases.ChangePassword((ulong)id, password)
            : new OperationResult(false, "Error");
    }

    public static OperationResult GetUserStatus(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? databases.GetUserStatus((ulong)id) : new OperationResult(false, "Error");
    }

    public static OperationResult GetUserAuthType(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? databases.GetUserAuthType((ulong)id) : new OperationResult(false, "Error");
    }

    public static OperationResult CreateAuthCodes(ulong? id, AuthType type, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? databases.CreateAuthCodes((ulong)id, type) : new OperationResult(false, "Error");
    }

    public static OperationResult GetUserContact(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? databases.GetUserContact((ulong)id) : new OperationResult(false, "Error");
    }

    public static OperationResult CheckAuthCode(ulong? id, int code, string type, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? databases.CheckAuthCode((ulong)id, code, type) : new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> Activate(ulong? id, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return id != null ? await databases.Activate((ulong)id) : new OperationResult(false, "Error");
    }

    public static OperationResult CheckLoginData(string? username, string? password, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return username != null && password != null ?
            databases.CheckLoginData(username, password) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> UpdatePassword(string? accessToken, string? newPassword, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && newPassword != null ?
            await databases.UpdatePassword(accessToken, newPassword) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> UpdateEmail(string? accessToken, string? newEmail, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && newEmail != null ?
            await databases.UpdateEmail(accessToken, newEmail) :
            new OperationResult(false, "Error");
    }

    public static async Task<OperationResult> UpdatePhone(string? accessToken, string? phoneCountry, string? phoneNumber, DatabasesManager? databases = null)
    {
        if (databases == null) databases = DatabasesBase;

        return accessToken != null && phoneCountry != null && phoneNumber != null ?
            await databases.UpdatePhone(accessToken, phoneCountry, phoneNumber) :
            new OperationResult(false, "Error");
    }
}
