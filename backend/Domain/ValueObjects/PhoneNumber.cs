using System;
using System.Text.RegularExpressions;

namespace ChatApp.Domain.ValueObjects;

public readonly record struct PhoneNumber
{
    private static readonly Regex PhoneRegex = new(
        @"^\\+?[1-9]\\d{1,14}$",
        RegexOptions.Compiled);

    public string Value { get; }

    public PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Phone number cannot be empty", nameof(input));

        var cleaned = Clean(input);
        
        if (!PhoneRegex.IsMatch(cleaned))
            throw new ArgumentException("Invalid phone number format. Use E.164 format (e.g., +1234567890)", nameof(input));

        return new PhoneNumber(cleaned);
    }

    public static bool TryParse(string input, out PhoneNumber phoneNumber)
    {
        try
        {
            phoneNumber = Parse(input);
            return true;
        }
        catch
        {
            phoneNumber = default;
            return false;
        }
    }

    private static string Clean(string input)
    {
        var cleaned = Regex.Replace(input, @"[\\s\\-()]", "");
        
        if (!cleaned.StartsWith("+"))
        {
            cleaned = "+" + cleaned;
        }

        return cleaned;
    }

    public string GetDisplayFormat()
    {
        if (Value.Length >= 10)
        {
            var countryCode = Value.Substring(1, 2);
            var areaCode = Value.Substring(3, 3);
            var prefix = Value.Substring(6, 3);
            var lineNumber = Value.Substring(9);
            return $"+{countryCode} ({areaCode}) {prefix}-{lineNumber}";
        }
        return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
    public static implicit operator PhoneNumber(string value) => Parse(value);
}
