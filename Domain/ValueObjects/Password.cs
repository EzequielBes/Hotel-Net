using System.Text.RegularExpressions;

namespace CheckInApp.Domain.ValueObjects;
public record Password
{
    public string Value { get; }

    public Password(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Password cannot be empty.");

        if (value.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        if (!Regex.IsMatch(value, @"[A-Z]"))
            throw new ArgumentException("Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(value, @"[a-z]"))
            throw new ArgumentException("Password must contain at least one lowercase letter.");

        if (!Regex.IsMatch(value, @"[0-9]"))
            throw new ArgumentException("Password must contain at least one digit.");

        if (!Regex.IsMatch(value, @"[\W_]"))
            throw new ArgumentException("Password must contain at least one special character.");

        Value = value;
    }
}