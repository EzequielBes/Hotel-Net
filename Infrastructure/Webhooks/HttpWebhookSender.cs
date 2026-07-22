using System.Net.Http.Json;
using CheckInApp.Domain.Entities;
using CheckInApp.Domain.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CheckInApp.Infrastructure.Webhooks;

public class HttpWebhookSender : IWebhookSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpWebhookSender> _logger;

    public HttpWebhookSender(HttpClient httpClient, IConfiguration configuration, ILogger<HttpWebhookSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public void SendBookingResult(BookingOrder order)
    {
        var url = _configuration["Webhooks:BookingConfirmed"];
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            var payload = new
            {
                order.Id,
                order.Status,
                order.RoomId,
                order.TotalPrice,
                order.Cpf
            };

            _httpClient.PostAsJsonAsync(url, payload).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking webhook for order {OrderId}", order.Id);
        }
    }
}
