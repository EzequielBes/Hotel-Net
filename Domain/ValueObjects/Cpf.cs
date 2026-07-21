using System.Text.RegularExpressions;

namespace CheckInApp.Domain.ValueObjects;
public record Cpf
{
    public string Value { get; }

    public Cpf(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CPF cannot be empty.");

        if (!Regex.IsMatch(value, @"^\d{3}\.\d{3}\.\d{3}-\d{2}$"))
            throw new ArgumentException("Invalid CPF format. Expected format: XXX.XXX.XXX-XX");

        Value = value;
    }
}