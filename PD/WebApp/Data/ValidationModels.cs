using System.ComponentModel.DataAnnotations;

namespace WebApp.Data;

public class RegistrationModel : DatabaseConnected
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

    public virtual string? Email { get; set; }
    public virtual string? PhoneCountry { get; set; }
    public virtual string? PhoneNumber { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_password_confirmation_is_required_")]
    [Compare("Password", 
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "Passwords_must_be_equal_")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    [Range(typeof(bool), "true", "true", 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Acceptance_of_the_terms_of_service_is_required_")]
    public bool TermsOfService { get; set; } = false;

    public virtual bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
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
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_email_is_required_")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5, 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_email_address_must_be_between_5_and_320_characters_long_")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$", 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Enter_a_valid_email_address_")]
    [DataType(DataType.EmailAddress)]
    public override string? Email { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class RegistrationModelSms : RegistrationModel
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_country_phone_code_is_required_")]
    [RegularExpression("^\\+[0-9]{1,3}$",
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "The_country_phone_code_must_have_1_3_digits_and_be_preceded_by_a_____sign_")]
    public override string? PhoneCountry { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_phone_number_is_required_")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "The_phone_number_must_have_4_12_digits_")]
    [DataType(DataType.PhoneNumber)]
    public override string? PhoneNumber { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class RegistrationModelBoth : RegistrationModel
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_email_is_required_")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5, 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_email_address_must_be_between_5_and_320_characters_long_")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$",
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Enter_a_valid_email_address_")]
    [DataType(DataType.EmailAddress)]
    public override string? Email { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_country_phone_code_is_required_")]
    [RegularExpression("^\\+[0-9]{1,3}$",
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "The_country_phone_code_must_have_1_3_digits_and_be_preceded_by_a_____sign_")]
    public override string? PhoneCountry { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_phone_number_is_required_")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessageResourceType = typeof(Translations), 
        ErrorMessageResourceName = "The_phone_number_must_have_4_12_digits_")]
    [DataType(DataType.PhoneNumber)]
    public override string? PhoneNumber { get; set; }

    public override bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class PasswordValidation
{
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
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class UsernameValidation
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

    public virtual bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class EmailValidation
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_email_is_required_")]
    [StringLength(maximumLength: 320,
        MinimumLength = 5, 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_email_address_must_be_between_5_and_320_characters_long_")]
    [RegularExpression("^[A-Za-z0-9]+[A-Za-z0-9_.-]*@[A-Za-z0-9]+[A-Za-z0-9.-]*$", 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "Enter_a_valid_email_address_")]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}

public class PhoneValidation
{
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_country_phone_code_is_required_")]
    [RegularExpression("^\\+[0-9]{1,3}$", 
        ErrorMessageResourceType = typeof(Translations),
        ErrorMessageResourceName = "The_country_phone_code_must_have_1_3_digits_and_be_preceded_by_a_____sign_")]
    public string? PhoneCountry { get; set; }

    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_phone_number_is_required_")]
    [RegularExpression("^[0-9]{4,12}$",
        ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = "The_phone_number_must_have_4_12_digits_")]
    [DataType(DataType.PhoneNumber)]
    public string? PhoneNumber { get; set; }

    public bool IsValid
    {
        get
        {
            var validationContext = new ValidationContext(this);
            return Validator.TryValidateObject(this, validationContext, null, true);
        }
    }
}
