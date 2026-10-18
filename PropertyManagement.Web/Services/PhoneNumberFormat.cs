namespace PropertyManagement.Web.Services;

// US 10-digit phone numbers, stored and shown as (555) 444-2912. Input may
// use any punctuation; a leading country code 1 is dropped.
public static class PhoneNumberFormat
{
    // Exactly 10 digits (optionally after a leading 1), any other characters
    // between them. Also used for client-side validation.
    public const string Pattern = @"^\D*(?:1\D*)?(?:\d\D*){10}$";

    public const string ErrorMessage = "Enter a 10-digit phone number.";

    // Null when the input does not contain a 10-digit US number.
    public static string? Normalize(string? input)
    {
        if (input is null)
        {
            return null;
        }

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        return digits.Length == 10
            ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
            : null;
    }

    // Formats stored values for display, leaving anything else unchanged.
    public static string Display(string? value) =>
        Normalize(value) ?? value ?? string.Empty;
}
