using System.Net;
using System.Net.Http.Json;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Integration.Tests;

[Collection("IntegrationTests")] // Ensure tests run sequentially to avoid conflicts with shared state (e.g., in-memory database)
public class PaymentsQueryControllerShould(TestApplicationFactory factory) : IClassFixture<TestApplicationFactory>
{
    private readonly Random _random = new();
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberMasked = _random.Next(1111, 9999).ToString(),
            Currency = "GBP",
            CreatedAt =  DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Status = PaymentStatus.Authorized,
        };

        await  factory.PaymentsRepository.AddAsync(payment);

        // Act
        var response = await _client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        // No need to arrange anything here as we are testing for a non-existent payment.

        // Act
        var response = await _client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}