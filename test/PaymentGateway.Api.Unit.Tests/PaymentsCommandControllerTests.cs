using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Unit.Tests;

public class PaymentsCommandControllerTests
{
    const string Skip = "Old tests for reference";
    private readonly Random _random = new();


    [Fact(Skip = Skip)]
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

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.AddAsync(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsCommandController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact(Skip = Skip)]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsCommandController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}