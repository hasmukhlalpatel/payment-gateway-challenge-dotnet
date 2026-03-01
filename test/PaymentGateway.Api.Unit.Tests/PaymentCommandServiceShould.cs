using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services.Commands;
using PaymentGateway.Api.Services.External;
using PaymentGateway.Core.Enums;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Unit.Tests;

public class PaymentCommandServiceShould
{
    [Fact]
    public async Task PersistsAndReturnsAuthorizedWhenBankAuthorizes()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var bankAdapterMock = new Mock<IBankAdapterService>();
        bankAdapterMock.Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankAuthorizationRequest>()))
            .ReturnsAsync(new BankAuthorizationResponse { Authorized = true, AuthorizationCode = "AUTH" });

        var logger = Mock.Of<ILogger<PaymentCommandService>>();
        var service = new PaymentCommandService(repository, bankAdapterMock.Object, logger);

        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424242",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 500,
            Cvv = "123"
        };

        // Act
        var result = await service.ExecutePaymentAsync(request);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, result.Status);
        var stored = await repository.GetAsync(result.Id);
        Assert.NotNull(stored);
        Assert.Equal(PaymentStatus.Authorized, stored!.Status);
        //TODO: Verify bank adapter was called with correct parameters
        //TODO: Verify that the card number was masked before being sent to the bank adapter
        //TODO: Verify mapping fields
    }

    [Fact]
    public async Task ThrowsPaymentExceptionWhenBankThrowsBankException()
    {
        // Arrange
        var repository = new PaymentsRepository();
        var bankAdapterMock = new Mock<IBankAdapterService>();
        bankAdapterMock.Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankAuthorizationRequest>()))
            .ThrowsAsync(new BankException("bank error"));

        var logger = Mock.Of<ILogger<PaymentCommandService>>();
        var service = new PaymentCommandService(repository, bankAdapterMock.Object, logger);

        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424242",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 500,
            Cvv = "123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<PaymentException>(() => service.ExecutePaymentAsync(request));
        //TODO: Verify bank adapter was called with correct parameters
    }
}