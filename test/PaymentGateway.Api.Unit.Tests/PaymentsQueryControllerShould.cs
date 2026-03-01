using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Queries;
using PaymentGateway.Api.Services.Queries;
using PaymentGateway.Core.Enums;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Unit.Tests;

public class PaymentsQueryControllerShould
{
    [Fact]
    public async Task ReturnsOk_WhenPaymentFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var expected = new PaymentDetailsResponse
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            Amount = 100,
            Currency = "GBP",
            CardNumberLastFour = "****",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Timestamp = DateTime.UtcNow
        };

        var queryServiceMock = new Mock<IPaymentQueryService>();
        queryServiceMock.Setup(s => s.GetStatusAsync(paymentId)).ReturnsAsync(expected);

        var logger = Mock.Of<ILogger<PaymentsQueryController>>();
        var controller = new PaymentsQueryController(queryServiceMock.Object, logger);

        // Act
        var actionResult = await controller.GetPaymentAsync(paymentId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenServiceThrowsPaymentException()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var queryServiceMock = new Mock<IPaymentQueryService>();
        queryServiceMock.Setup(s => s.GetStatusAsync(paymentId)).ThrowsAsync(new PaymentException("not found"));

        var logger = Mock.Of<ILogger<PaymentsQueryController>>();
        var controller = new PaymentsQueryController(queryServiceMock.Object, logger);

        // Act
        var actionResult = await controller.GetPaymentAsync(paymentId);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var serialized = JsonSerializer.Serialize(notFound.Value);
        Assert.Contains("not found", serialized, StringComparison.OrdinalIgnoreCase);
    }
}