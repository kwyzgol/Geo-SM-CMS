namespace Tests.UnitTests;

[TestFixture]
public class ValidationTests
{
    [Test]
    [TestCase("123456", true)]
    [TestCase("100000", true)]
    [TestCase("999999", true)]
    [TestCase("1000000", false)]
    [TestCase("999", false)]
    [TestCase("99999", false)]
    [TestCase("abcdef", false)]
    [TestCase("abc", false)]
    [TestCase("", false)]
    public void AuthCodeEmail_IsValid_ProvidedData_ReturnsStatus(string code, bool expected)
    {
        var authCodeEmail = new AuthCodeEmail() { CodeEmail = code };

        var result = authCodeEmail.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("123456", true)]
    [TestCase("100000", true)]
    [TestCase("999999", true)]
    [TestCase("1000000", false)]
    [TestCase("999", false)]
    [TestCase("99999", false)]
    [TestCase("abcdef", false)]
    [TestCase("abc", false)]
    [TestCase("", false)]
    public void AuthCodeSms_IsValid_ProvidedData_ReturnsStatus(string code, bool expected)
    {
        var authCodeSms = new AuthCodeSms() { CodeSms = code };

        var result = authCodeSms.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("123456", "123456", true)]
    [TestCase("123456", "", false)]
    [TestCase("", "123456", false)]
    [TestCase("100000", "100000", true)]
    [TestCase("100000", "", false)]
    [TestCase("", "100000", false)]
    [TestCase("999999", "999999", true)]
    [TestCase("999999", "", false)]
    [TestCase("", "999999", false)]
    [TestCase("1000000", "1000000", false)]
    [TestCase("123456", "1000000", false)]
    [TestCase("1000000", "123456", false)]
    [TestCase("999", "999", false)]
    [TestCase("123456", "999", false)]
    [TestCase("999", "123456", false)]
    [TestCase("99999", "99999", false)]
    [TestCase("123456", "99999", false)]
    [TestCase("99999", "123456", false)]
    [TestCase("abcdef", "abcdef", false)]
    [TestCase("123456", "abcdef", false)]
    [TestCase("abcdef", "123456", false)]
    [TestCase("abc", "abc", false)]
    [TestCase("123456", "abc", false)]
    [TestCase("abc", "123456", false)]
    [TestCase("", "", false)]
    public void AuthCodeBoth_IsValid_ProvidedData_ReturnsStatus(string codeEmail, string codeSms, bool expected)
    {
        var authCodeBoth = new AuthCodesBoth()
        {
            CodeEmail = codeEmail,
            CodeSms = codeSms
        };

        var result = authCodeBoth.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("Lorem ipsum", true)]
    [TestCase("", false)]
    [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus volutpat libero maximus, condimentum velit lacinia, finibus risus. Cras in pharetra enim. Mauris sagittis odio ac ligula ultrices efficitur. Vivamus eleifend feugiat dictum. Sed ullamcorper turpis bibendum, tempus erat id, molestie nunc. Praesent erat mauris, volutpat scelerisque velit id, ultricies sodales tellus. Proin luctus sapien et orci mollis finibus. Cras ultrices iaculis est, sed venenatis enim. Etiam rutrum lorem vel mattis malesuada. Nam a molestie ligula. Sed iaculis feugiat mattis. Vivamus pulvinar maximus eros, vitae aliquet arcu dapibus iaculis. Cras mattis, velit ut porttitor sodales, ligula nisi ultrices ante, fermentum luctus mi leo id enim. Duis eu quam augue. Nunc mollis lacinia lacus vel egestas. Donec commodo augue tellus, eget ultricies orci molestie eget. Vestibulum quis mi in ex bibendum imperdiet non sit amet dolor. Maecenas quis cursus mauris. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Sed vehicula odio bibendum neque lobortis, at iaculis dui facilisis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Mauris bibendum justo nulla. Integer eu tortor id orci luctus euismod. Maecenas iaculis facilisis accumsan. Ut aliquet sollicitudin lacus, non egestas elit aliquet ac. Aliquam id consequat nunc. Pellentesque non ex in erat sollicitudin bibendum. Quisque suscipit, turpis eget condimentum scelerisque, est sem convallis orci, non viverra erat est placerat diam. Fusce sollicitudin aliquam sem, ac vehicula erat tristique eget. Interdum et malesuada fames ac ante ipsum primis in faucibus. Pellentesque semper euismod tincidunt. Maecenas magna sem, tristique eget hendrerit aliquam, feugiat id arcu. Vivamus laoreet nibh nec blandit porta. Ut consectetur imperdiet gravida. Praesent eu auctor mauris, quis aliquam quam. Fusce convallis vitae felis pharetra. Suspendisse potenti. Fusce sit amet quis.",
    true, TestName = "CommentModel_IsValid_ProvidedData_ReturnsStatus(Length: 2000, true)")]
    [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus volutpat libero maximus, condimentum velit lacinia, finibus risus. Cras in pharetra enim. Mauris sagittis odio ac ligula ultrices efficitur. Vivamus eleifend feugiat dictum. Sed ullamcorper turpis bibendum, tempus erat id, molestie nunc. Praesent erat mauris, volutpat scelerisque velit id, ultricies sodales tellus. Proin luctus sapien et orci mollis finibus. Cras ultrices iaculis est, sed venenatis enim. Etiam rutrum lorem vel mattis malesuada. Nam a molestie ligula. Sed iaculis feugiat mattis. Vivamus pulvinar maximus eros, vitae aliquet arcu dapibus iaculis. Cras mattis, velit ut porttitor sodales, ligula nisi ultrices ante, fermentum luctus mi leo id enim. Duis eu quam augue. Nunc mollis lacinia lacus vel egestas. Donec commodo augue tellus, eget ultricies orci molestie eget. Vestibulum quis mi in ex bibendum imperdiet non sit amet dolor. Maecenas quis cursus mauris. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Sed vehicula odio bibendum neque lobortis, at iaculis dui facilisis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Mauris bibendum justo nulla. Integer eu tortor id orci luctus euismod. Maecenas iaculis facilisis accumsan. Ut aliquet sollicitudin lacus, non egestas elit aliquet ac. Aliquam id consequat nunc. Pellentesque non ex in erat sollicitudin bibendum. Quisque suscipit, turpis eget condimentum scelerisque, est sem convallis orci, non viverra erat est placerat diam. Fusce sollicitudin aliquam sem, ac vehicula erat tristique eget. Interdum et malesuada fames ac ante ipsum primis in faucibus. Pellentesque semper euismod tincidunt. Maecenas magna sem, tristique eget hendrerit aliquam, feugiat id arcu. Vivamus laoreet nibh nec blandit porta. Ut consectetur imperdiet gravida. Praesent eu auctor mauris, quis aliquam quam. Fusce convallis vitae felis pharetra. Suspendisse potenti. Fusce sit amet quis.1",
    false, TestName = "CommentModel_IsValid_ProvidedData_ReturnsStatus(Length: 2001, false)")]
    public void CommentModel_IsValid_ProvidedData_ReturnsStatus(string contentParam, bool expected)
    {
        var commentModel = new CommentModel() { Content = contentParam };

        var result = commentModel.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("Lorem ipsum", true)]
    [TestCase("", false)]
    [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin at metus erat. Nullam faucibus libero vel eros vestibulum tempus. Vestibulum euismod tellus eu est aliquam, eu rhoncus nulla efficitur. Morbi placerat arcu eros, in egestas mauris tincidunt id. Curabitur tincidunt, nibh at dignissim vehicula, quam nisl eleifend est, nec ultrices odio magna non mauris. Nunc non orci orci. Suspendisse et venenatis ex, quis mattis ex. Duis tincidunt leo semper lacus volutpat, eget aliquet enim eleifend. Etiam luctus mauris non orci vestibulum condimentum. Nulla ac ullamcorper lorem. Vestibulum pretium magna augue, in suscipit diam lacinia ut. Etiam sit amet tempus massa. Sed massa orci, malesuada vitae ipsum sed, tempor egestas ante. Cras accumsan molestie condimentum. Aliquam pretium mauris ut diam imperdiet porttitor. Nulla varius eros sit amet orci lacinia maximus. Quisque tincidunt condimentum consectetur. Nulla sit amet ornare diam. Phasellus pretium molestie arcu. Na at libero ut mauris.",
        true, TestName = "MessageModel_IsValid_ProvidedData_ReturnsStatus(Length: 1000, true)")]
    [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin at metus erat. Nullam faucibus libero vel eros vestibulum tempus. Vestibulum euismod tellus eu est aliquam, eu rhoncus nulla efficitur. Morbi placerat arcu eros, in egestas mauris tincidunt id. Curabitur tincidunt, nibh at dignissim vehicula, quam nisl eleifend est, nec ultrices odio magna non mauris. Nunc non orci orci. Suspendisse et venenatis ex, quis mattis ex. Duis tincidunt leo semper lacus volutpat, eget aliquet enim eleifend. Etiam luctus mauris non orci vestibulum condimentum. Nulla ac ullamcorper lorem. Vestibulum pretium magna augue, in suscipit diam lacinia ut. Etiam sit amet tempus massa. Sed massa orci, malesuada vitae ipsum sed, tempor egestas ante. Cras accumsan molestie condimentum. Aliquam pretium mauris ut diam imperdiet porttitor. Nulla varius eros sit amet orci lacinia maximus. Quisque tincidunt condimentum consectetur. Nulla sit amet ornare diam. Phasellus pretium molestie arcu. Na at libero ut mauris.1",
        false, TestName = "MessageModel_IsValid_ProvidedData_ReturnsStatus(Length: 1001, false)")]
    public void MessageModel_IsValid_ProvidedData_ReturnsStatus(string contentParam, bool expected)
    {
        var messageModel = new MessageModel() { Content = contentParam };

        var result = messageModel.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("+48", 9, "+48123456789", true)]
    [TestCase("+48", 9, "+00123456789", false)]
    [TestCase("+48", 9, "+4812345678", false)]
    [TestCase("+48", 9, "+481234567890", false)]
    [TestCase("+48", 9, "48123456789", false)]
    [TestCase("+0", 5, "+012345", true)]
    [TestCase("+48", 9, "", false)]
    public void PhoneAllowed_Verify_ProvidedData_ReturnsStatus(string countryCode, int maxNumbers, string phoneNumberParam, bool expected)
    {
        var phoneAllowed = new PhoneAllowed(countryCode, maxNumbers);

        var result = phoneAllowed.Verify(phoneNumberParam);

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test", "12345678", true)]
    [TestCase("123456789012345678901", "12345678", false)]
    [TestCase("test", "1234567", false)]
    [TestCase("test", "@!$%^&*<>_.,?abcABC123", true)]
    [TestCase("test", "12 34 56 78", false)]
    [TestCase("", "", false)]
    [TestCase(null, null, false)]
    [TestCase("test", "max_length_password_WFXseYUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt", true)]
    [TestCase("test", "over_max_length_password_YUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt1", false)]
    [TestCase("te._st", "12345678", true)]
    [TestCase("te%/st", "12345678", false)]
    public void UserAccessModel_IsValid_ProvidedData_ReturnsStatus(string username, string password, bool expected)
    {
        var userAccessModel = new UserAccessModel() { Username = username, Password = password };

        var result = userAccessModel.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test", "12345678", "12345678", true, true)]
    [TestCase("123456789012345678901", "12345678", "12345678", true, false)]
    [TestCase("teST._123", "12345678", "12345678", true, true)]
    [TestCase("te%/st", "12345678", "12345678", true, false)]
    [TestCase("test", "1234567", "1234567", true, false)]
    [TestCase("test", "max_length_password_WFXseYUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt",
        "max_length_password_WFXseYUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt", true, true)]
    [TestCase("test", "over_max_length_password_YUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt1",
        "over_max_length_password_YUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt1", true, false)]
    [TestCase("test", "@!$%^&*<>_.,?abcABC123", "@!$%^&*<>_.,?abcABC123", true, true)]
    [TestCase("test", "12345678", "87654321", true, false)]
    [TestCase("test", "12345678", "12345678", false, false)]
    [TestCase("", "", "", true, false)]
    [TestCase(null, null, null, false, false)]
    public void RegistrationModel_IsValid_ProvidedData_ReturnsStatus
        (string username, string password, string confirmPassword, bool termsOfService, bool expected)
    {
        var registrationModel = new RegistrationModel()
        {
            Username = username,
            Password = password,
            ConfirmPassword = confirmPassword,
            TermsOfService = termsOfService
        };

        var result = registrationModel.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test@example.test", true)]
    [TestCase("a@a.a", true)]
    [TestCase("max_length_email@example123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901.test",
        true)]
    [TestCase("over_max_length_email@example1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456.test1",
        false)]
    [TestCase("test", false)]
    [TestCase("test@example.test#$%^&", false)]
    [TestCase("", false)]
    public void RegistrationModelEmail_IsValid_ProvidedData_ReturnsStatus(string email, bool expected)
    {
        var registrationModelEmail = new RegistrationModelEmail()
        {
            Username = "test",
            Password = "12345678",
            ConfirmPassword = "12345678",
            TermsOfService = true,
            Email = email
        };

        var result = registrationModelEmail.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("+0", "1234", true)]
    [TestCase("+123", "123456789012", true)]
    [TestCase("+", "1234", false)]
    [TestCase("+0", "123", false)]
    [TestCase("+0", "1234567890123", false)]
    [TestCase("+1234", "1234", false)]
    [TestCase("", "", false)]
    public void RegistrationModelSms_IsValid_ProvidedData_ReturnsStatus(string phoneCountry, string phoneNumber, bool expected)
    {
        var registrationModelSms = new RegistrationModelSms()
        {
            Username = "test",
            Password = "12345678",
            ConfirmPassword = "12345678",
            TermsOfService = true,
            PhoneCountry = phoneCountry,
            PhoneNumber = phoneNumber
        };

        var result = registrationModelSms.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test@example.test", "+0", "1234", true)]
    [TestCase("a@a.a", "+0", "1234", true)]
    [TestCase("max_length_email@example123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901.test",
        "+0", "1234", true)]
    [TestCase("over_max_length_email@example1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456.test1",
        "+0", "1234", false)]
    [TestCase("test", "+0", "1234", false)]
    [TestCase("test@example.test#$%^&","+0", "1234", false)]
    [TestCase("", "", "", false)]
    [TestCase("test@example.test", "+123", "123456789012", true)]
    [TestCase("test@example.test", "+", "1234", false)]
    [TestCase("test@example.test", "+0", "123", false)]
    [TestCase("test@example.test", "+0", "1234567890123", false)]
    [TestCase("test@example.test", "+1234", "1234", false)]
    public void RegistrationModelBoth_IsValid_ProvidedData_ReturnsStatus(string email, string phoneCountry, string phoneNumber, bool expected)
    {
        var registrationModelBoth = new RegistrationModelBoth()
        {
            Username = "test",
            Password = "12345678",
            ConfirmPassword = "12345678",
            TermsOfService = true,
            Email = email,
            PhoneCountry = phoneCountry,
            PhoneNumber = phoneNumber
        };

        var result = registrationModelBoth.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("12345678", true)]
    [TestCase("1234567", false)]
    [TestCase("max_length_password_WFXseYUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt", true)]
    [TestCase("over_max_length_password_YUDoUihEtXnBxYRitNRateUZEaHQHQGidnWgZCt1", false)]
    [TestCase("@!$%^&*<>_.,?abcABC123", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void PasswordValidation_IsValid_ProvidedData_ReturnsStatus(string password, bool expected)
    {
        var passwordValidation = new PasswordValidation(){ Password = password };

        var result = passwordValidation.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test", true)]
    [TestCase("12345678901234567890", true)]
    [TestCase("123456789012345678901", false)]
    [TestCase("teST._123", true)]
    [TestCase("te%/st", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void UsernameValidation_IsValid_ProvidedData_ReturnsStatus(string username, bool expected)
    {
        var usernameValidation = new UsernameValidation() { Username = username };

        var result = usernameValidation.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("test@example.test", true)]
    [TestCase("a@a.a", true)]
    [TestCase("max_length_email@example123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901.test",
        true)]
    [TestCase("over_max_length_email@example1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456.test1",
        false)]
    [TestCase("test", false)]
    [TestCase("test@example.test#$%^&", false)]
    [TestCase("", false)]
    public void EmailValidation_IsValid_ProvidedData_ReturnsStatus(string email, bool expected)
    {
        var emailValidation = new EmailValidation() { Email = email };

        var result = emailValidation.IsValid;

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase("+0", "1234", true)]
    [TestCase("+123", "123456789012", true)]
    [TestCase("+", "1234", false)]
    [TestCase("+0", "123", false)]
    [TestCase("+0", "1234567890123", false)]
    [TestCase("+1234", "1234", false)]
    [TestCase("", "", false)]
    public void PhoneValidation_IsValid_ProvidedData_ReturnsStatus(string phoneCountry, string phoneNumber, bool expected)
    {
        var phoneValidation = new PhoneValidation() { PhoneCountry = phoneCountry, PhoneNumber = phoneNumber };

        var result = phoneValidation.IsValid;

        Assert.AreEqual(expected, result);
    }
}
