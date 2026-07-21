using System.Text.Json.Serialization;

namespace CheckInApp.Domain.Entities;

public class Hospede
{
    [JsonPropertyName("Cpf")]
    public string Cpf { get; set; } = string.Empty;

    [JsonPropertyName("nomeHospede")]
    public string Nome { get; set; } = string.Empty;
}
