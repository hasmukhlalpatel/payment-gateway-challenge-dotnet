using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Moq;

using PaymentGateway.Api.Services.External;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Unit.Tests;

public class BankAdapterServiceShould
{
    private class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public TestHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    [Fact]
    public async Task ReturnsResponseWhenHttpOk()
    {
        // Arrange
        var dto = new BankAuthorizationResponse { Authorized = true, AuthorizationCode = "AUTH123" };
        var json = JsonSerializer.Serialize(dto);

        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://bank") };
        var logger = Mock.Of<ILogger<BankAdapterService>>();
        var service = new BankAdapterService(client, logger);

        // Act
        var result = await service.AuthorizePaymentAsync(new BankAuthorizationRequest
        {
            CardNumber = "4242424242424242",
            ExpiryDate = "12/2030",
            Cvv = "123",
            Amount = 100,
            Currency = "GBP"
        });

        // Assert
        Assert.True(result.Authorized);
        Assert.Equal("AUTH123", result.AuthorizationCode);
        //TODO: Verify the request was made to the correct endpoint with expected content
    }

    [Fact]
    public async Task ThrowsBankExceptionWhenNonSuccessStatus()
    {
        // Arrange
        var handler = new TestHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://bank") };
        var logger = Mock.Of<ILogger<BankAdapterService>>();
        var service = new BankAdapterService(client, logger);

        // Act & Assert
        await Assert.ThrowsAsync<BankException>(() => service.AuthorizePaymentAsync(new BankAuthorizationRequest
        {
            CardNumber = "1",
            ExpiryDate = "01/2000",
            Cvv = "1",
            Amount = 1,
            Currency = "USD"
        }));
    }
}