using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class RegistrationModel : DatabaseConnected
{
    [Required(ErrorMessage = "The username is required.")]
    [StringLength(maximumLength: 20,
        MinimumLength = 4,
        ErrorMessage = "The username must be between 4 and 20 characters long.")]
    [RegularExpression("^[A-Za-z0-9_.]{4,20}$",
        ErrorMessage = "Available characters for the username: A-Z, a-z, 0-9, ._")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "The password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "The password must be between 8 and 64 characters long.")]
    [RegularExpression("^[A-Za-z0-9!@$%^&*<>_.,?-]{8,64}$",
        ErrorMessage = "Available characters for the password: A-Z, a-z, 0-9, !@$%^&*<>_.,?-")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public virtual string? Email { get; set; }
    public virtual string? PhoneCountry { get; set; }
    public virtual string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "The password confirmation is required.")]
    [Compare("Password", ErrorMessage = "Passwords must be equal.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    [Range(typeof(bool), "true", "true",
        ErrorMessage = "Acceptance of the terms of service is required.")]
    public bool TermsOfService { get; set; } = false;

    public virtual bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }

    public OperationResult CheckUsernameExists()
    {
        return Username != null ? 
            Databases.CheckUsernameExists(Username) : 
            new OperationResult(false, "Error");
    }

    public OperationResult CheckEmailExists()
    {
        return Email != null ?
            Databases.CheckEmailExists(Email) :
            new OperationResult(false, "Error");
    }

    public OperationResult CheckPhoneNumberExists()
    {
        return PhoneCountry != null && PhoneNumber != null ?
            Databases.CheckPhoneNumberExists(PhoneCountry, PhoneNumber) : 
            new OperationResult(false, "Error");
    }

    public OperationResult Register(bool createAdmin = false)
    {
        return IsValid ? Databases.Register(this, createAdmin) : 
            new OperationResult(false, "Error");
    }
}

public class RegistrationModelEmail : RegistrationModel
{
    [Required(ErrorMessage = "The email is required.")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5,
        ErrorMessage = "The email address must be between 5 and 320 characters long.")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$",
        ErrorMessage = "Enter a valid email address.")]
    [DataType(DataType.EmailAddress)]
    public override string? Email { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

public class RegistrationModelSms : RegistrationModel
{
    [Required(ErrorMessage = "The country phone code is required.")]
    [RegularExpression("^\\+[0-9]{1,3}$",
        ErrorMessage = "The country phone code must have 1-3 digits and be preceded by a '+' sign.")]
    public override string? PhoneCountry { get; set; }

    [Required(ErrorMessage = "The phone number is required.")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessage = "The phone number must have 4-12 digits.")]
    [DataType(DataType.PhoneNumber)]
    public override string? PhoneNumber { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

public class RegistrationModelBoth : RegistrationModel
{
    [Required(ErrorMessage = "The email is required.")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5,
        ErrorMessage = "The email address must be between 5 and 320 characters long.")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$",
        ErrorMessage = "Enter a valid email address.")]
    [DataType(DataType.EmailAddress)]
    public override string? Email { get; set; }

    [Required(ErrorMessage = "The country phone code is required.")]
    [RegularExpression("^\\+[0-9]{1,3}$",
        ErrorMessage = "The country phone code must have 1-3 digits and be preceded by a '+' sign.")]
    public override string? PhoneCountry { get; set; }

    [Required(ErrorMessage = "The phone number is required.")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessage = "The phone number must have 4-12 digits.")]
    [DataType(DataType.PhoneNumber)]
    public override string? PhoneNumber { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

public class PasswordValidation
{
    [Required(ErrorMessage = "The password is required.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "The password must be between 8 and 64 characters long.")]
    [RegularExpression("^[A-Za-z0-9!@$%^&*<>_.,?-]{8,64}$",
        ErrorMessage = "Available characters for the password: A-Z, a-z, 0-9, !@$%^&*<>_.,?-")]
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
}

public class UsernameValidation
{
    [Required(ErrorMessage = "The username is required.")]
    [StringLength(maximumLength: 20,
        MinimumLength = 4,
        ErrorMessage = "The username must be between 4 and 20 characters long.")]
    [RegularExpression("^[A-Za-z0-9_.]{4,20}$",
        ErrorMessage = "Available characters for the username: A-Z, a-z, 0-9, ._")]
    public string? Username { get; set; }

    public virtual bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

public class EmailValidation
{
    [Required(ErrorMessage = "The email is required.")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5,
        ErrorMessage = "The email address must be between 5 and 320 characters long.")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$",
        ErrorMessage = "Enter a valid email address.")]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

public class PhoneValidation
{
    [Required(ErrorMessage = "The country phone code is required.")]
    [RegularExpression("^\\+[0-9]{1,3}$",
        ErrorMessage = "The country phone code must have 1-3 digits and be preceded by a '+' sign.")]
    public string? PhoneCountry { get; set; }

    [Required(ErrorMessage = "The phone number is required.")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessage = "The phone number must have 4-12 digits.")]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null);
        }
    }
}

/*
 * TODO testy walidacji
 */