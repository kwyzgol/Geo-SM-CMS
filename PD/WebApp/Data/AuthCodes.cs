using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public abstract class AuthCodes
{
    public virtual string CodeEmail { get; set; } = "";
    public virtual string CodeSms { get; set; } = "";
}

public class AuthCodeEmail : AuthCodes
{
    [Required(ErrorMessage = "The email auth code is required.")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessage = "Invalid email auth code format.")]
    public override string CodeEmail { get; set; } = "";
}

public class AuthCodeSms : AuthCodes
{
    [Required(ErrorMessage = "The SMS auth code is required.")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessage = "Invalid SMS auth code format.")]
    public override string CodeSms { get; set; } = "";
}

public class AuthCodesBoth : AuthCodes
{
    [Required(ErrorMessage = "The email auth code is required.")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessage = "Invalid email auth code format.")]
    public override string CodeEmail { get; set; } = "";

    [Required(ErrorMessage = "The SMS auth code is required.")]
    [RegularExpression("^[1-9]{1}[0-9]{5}$",
        ErrorMessage = "Invalid SMS auth code format.")]
    public override string CodeSms { get; set; } = "";
}


