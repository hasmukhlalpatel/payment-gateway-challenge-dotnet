using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Integration.Tests;

[Collection("IntegrationTests")] // Ensure tests run sequentially to avoid conflicts with shared state (e.g., in-memory database)
public class PaymentsCommandControllerShould(TestApplicationFactory factory) : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreatesPaymentSuccessfully()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424241", // This card number ends with 1,3,5,7,9 is set to be authorized in the imposters\bank_simulator.ejs
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse!.Status);

        var stored = await factory.PaymentsRepository.GetAsync(paymentResponse.Id);
        Assert.NotNull(stored);
        Assert.Equal(PaymentStatus.Authorized, stored!.Status);
    }

    [Fact]
    public async Task DeclinePayment()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424242", // This card number ends with 2,4,6,8,0 is set to be declined in the imposters\bank_simulator.ejs
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse!.Status);

        var stored = await factory.PaymentsRepository.GetAsync(paymentResponse.Id);
        Assert.NotNull(stored);
        Assert.Equal(PaymentStatus.Declined, stored!.Status);
    }

    [Fact]
    public async Task ReturnsBadRequestWhenValidationFails()
    {
        // Arrange - invalid expiry year (in the past) and missing card number
        var request = new PostPaymentRequest
        {
            CardNumber = string.Empty,
            ExpiryMonth = 1,
            ExpiryYear = 2000,
            Currency = "GBP",
            Amount = 100,
            Cvv = "12"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Payments", request);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("message", out var message));
        Assert.Equal("Validation failed", message.GetString());
        Assert.True(doc.RootElement.TryGetProperty("status", out var status));
        // Status should be serialized as string (JsonStringEnumConverter)
        Assert.Equal("Rejected", status.GetString());
    }
}
