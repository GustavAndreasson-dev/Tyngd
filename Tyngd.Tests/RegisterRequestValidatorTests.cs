using Tyngd.Shared.DTOs;

namespace Tyngd.Tests;

public class RegisterRequestValidatorTests
{
    [Theory]
    [InlineData("anna", "anna@exempel.se", "hemligt", "hemligt", null)]
    [InlineData("anna", "anna@exempel.se", "hemligt", " ", "Fyll i alla fält.")]
    [InlineData(" an ", "anna@exempel.se", "hemligt", "hemligt", "Användarnamnet måste vara minst 3 tecken.")]
    [InlineData("anna", "anna@exempel", "hemligt", "hemligt", "Ange en giltig e-postadress.")]
    [InlineData("anna", "@exempel.se", "hemligt", "hemligt", "Ange en giltig e-postadress.")]
    [InlineData("anna", "anna@.se", "hemligt", "hemligt", "Ange en giltig e-postadress.")]
    [InlineData("anna", "anna@exempel.se", "kort", "kort", "Lösenordet måste vara minst 6 tecken.")]
    [InlineData("anna", "anna@exempel.se", "hemligt", "hemligt2", "Lösenorden matchar inte.")]
    public void Returns_the_first_error(string userName, string email, string password, string confirm, string? expected) =>
        Assert.Equal(expected, RegisterRequestValidator.Validate(userName, email, password, confirm));
}
