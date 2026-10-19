using TalentValley.Api.DTOs;

namespace TalentValley.Api.Tests;

public sealed class InputValidationPhoneTests
{
    [Theory]
    [InlineData("(32) 99999-1234")]
    [InlineData("+55 (32) 99999-1234")]
    [InlineData("32999991234")]
    [InlineData("(11) 91234-5678")]
    [InlineData("(99) 91234-5678")]
    [InlineData("(32) 2333-4444")]
    [InlineData("(32) 3333-4444")]
    [InlineData("(32) 4333-4444")]
    [InlineData("(32) 5333-4444")]
    [InlineData("+55 32 3333-4444")]
    public void Valid_brazilian_phones_are_accepted(string phone) =>
        Assert.True(InputValidation.IsValidPhoneInput(phone));

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("11111111111")]
    [InlineData("0012345678")]
    [InlineData("(10) 91234-5678")]
    [InlineData("(01) 91234-5678")]
    [InlineData("(32) 88888-7777")]
    [InlineData("(32) 12345-6789")]
    [InlineData("(32) 1333-4444")]
    [InlineData("(32) 6333-4444")]
    [InlineData("(32) 9333-4444")]
    public void Phones_with_a_wrong_area_code_or_first_digit_are_rejected(string phone) =>
        Assert.False(InputValidation.IsValidPhoneInput(phone));
}
