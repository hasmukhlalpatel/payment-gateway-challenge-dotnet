using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Services.Commands;
using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Unit.Tests;

public class PaymentsCommandControllerShould
{
    [Fact]
    public async Task ReturnsCreatedWhenValidationSucceeds()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424242",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };

        var validatorMock = new Mock<IValidator<PostPaymentRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var response = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = "****",
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
            Timestamp = DateTime.UtcNow
        };

        var commandServiceMock = new Mock<IPaymentCommandService>();
        commandServiceMock.Setup(s => s.ExecutePaymentAsync(request)).ReturnsAsync(response);

        var logger = Mock.Of<ILogger<PaymentsCommandController>>();

        var controller = new PaymentsCommandController(validatorMock.Object, commandServiceMock.Object, logger);

        // Act
        var result = await controller.PostPaymentAsync(request);

        // Assert
        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task ReturnsBadRequestWhenValidationFails()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "",
            ExpiryMonth = 1,
            ExpiryYear = 2000,
            Currency = "GBP",
            Amount = 100,
            Cvv = "12"
        };

        var failure = new FluentValidation.Results.ValidationResult(new[] {
            new FluentValidation.Results.ValidationFailure("CardNumber", "Required")
        });

        var validatorMock = new Mock<IValidator<PostPaymentRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(request, default))
            .ReturnsAsync(failure);

        var commandServiceMock = new Mock<IPaymentCommandService>();
        var logger = Mock.Of<ILogger<PaymentsCommandController>>();

        var controller = new PaymentsCommandController(validatorMock.Object, commandServiceMock.Object, logger);

        // Act
        var result = await controller.PostPaymentAsync(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var serialized = JsonSerializer.Serialize(badRequest.Value, new JsonSerializerOptions()
        {
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.Contains("Validation failed", serialized);
        Assert.Contains("Rejected", serialized);
    }
}