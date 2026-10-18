using System.Text.RegularExpressions;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Tests.Services;

public class PhoneNumberFormatTests
{
    [Theory]
    [InlineData("5554442912")]
    [InlineData("555-444-2912")]
    [InlineData("555.444.2912")]
    [InlineData("(555) 444-2912")]
    [InlineData("(555)-444-2912")]
    [InlineData(" 555 444 2912 ")]
    [InlineData("1-555-444-2912")]
    [InlineData("+1 (555) 444-2912")]
    public void Normalize_TenDigitUsNumber_ReturnsStandardFormat(string input)
    {
        Assert.Equal("(555) 444-2912", PhoneNumberFormat.Normalize(input));
        Assert.Matches(PhoneNumberFormat.Pattern, input);
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("555444291")]
    [InlineData("25554442912")]
    [InlineData("555-444-29123")]
    [InlineData("")]
    [InlineData("phone")]
    public void Normalize_NotTenDigits_ReturnsNull(string input)
    {
        Assert.Null(PhoneNumberFormat.Normalize(input));
        Assert.DoesNotMatch(new Regex(PhoneNumberFormat.Pattern), input);
    }

    [Fact]
    public void Display_UnrecognizedValue_IsLeftUnchanged()
    {
        Assert.Equal("ext. 12", PhoneNumberFormat.Display("ext. 12"));
        Assert.Equal("(555) 444-2912", PhoneNumberFormat.Display("555-444-2912"));
    }
}
