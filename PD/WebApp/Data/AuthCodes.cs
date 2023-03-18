using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public abstract class AuthCodes
{
    public virtual string CodeEmail { get; set; } = "";
    public virtual string CodeSms { get; set; } = "";

    public virtual bool IsValid { get; set; } = false;
}

public class AuthCodeEmail : AuthCodes
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_email_auth_code_is_required_")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$", ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Invalid_email_auth_code_format_")]
    public override string CodeEmail { get; set; } = "";

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class AuthCodeSms : AuthCodes
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_SMS_auth_code_is_required_")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$", ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Invalid_SMS_auth_code_format_")]
    public override string CodeSms { get; set; } = "";

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class AuthCodesBoth : AuthCodes
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_email_auth_code_is_required_")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "Invalid_email_auth_code_format_")]
    public override string CodeEmail { get; set; } = "";

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_SMS_auth_code_is_required_")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "Invalid_SMS_auth_code_format_")]
    public override string CodeSms { get; set; } = "";

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}
