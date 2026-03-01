using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services.Queries;
using PaymentGateway.Core.Enums;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Unit.Tests;

public class PaymentQueryServiceShould
{
    [Fact]
    public async Task ReturnsDetailsWhenPaymentExists()
    {
        // Arrange
        var repo = new PaymentsRepository();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 200,
            Currency = "GBP",
            CardNumberMasked = "****",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Status = PaymentStatus.Authorized
        };
        await repo.AddAsync(payment);

        var logger = Mock.Of<ILogger<PaymentQueryService>>();
        var service = new PaymentQueryService(repo, logger);

        // Act
        var details = await service.GetStatusAsync(payment.Id);

        // Assert
        Assert.Equal(payment.Id, details.Id);
        Assert.Equal(payment.Status, details.Status);
        //TODO: Verify mapping of other fields
        //TODO: mock repository and verify GetAsync was called with correct payment id 
    }

    [Fact]
    public async Task ThrowsPaymentExceptionWhenNotFound()
    {
        // Arrange
        var repo = new PaymentsRepository();
        var logger = Mock.Of<ILogger<PaymentQueryService>>();
        var service = new PaymentQueryService(repo, logger);

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() => service.GetStatusAsync(Guid.NewGuid()));
    }
}