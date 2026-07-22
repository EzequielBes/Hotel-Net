using System.Text.Json.Serialization;

namespace CheckInApp.Domain.Entities;

public class Guest
{
    [JsonPropertyName("Cpf")]
    public string Cpf { get; set; } = string.Empty;

    [JsonPropertyName("guestName")]
    public string Name { get; set; } = string.Empty;
}
