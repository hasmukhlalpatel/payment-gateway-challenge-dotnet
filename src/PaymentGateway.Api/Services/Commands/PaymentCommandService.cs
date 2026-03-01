using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services.External;
using PaymentGateway.Core.Enums;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Services.Commands;

public interface IPaymentCommandService
{
    Task<PostPaymentResponse> ExecutePaymentAsync(PostPaymentRequest request);
}

public class PaymentCommandService(IPaymentsRepository repository, IBankAdapterService bankAdapter, ILogger<PaymentCommandService> logger)
    : IPaymentCommandService
{
    public async Task<PostPaymentResponse> ExecutePaymentAsync(PostPaymentRequest request)
    {
        logger.LogInformation($"Processing payment for amount {request.Amount} {request.Currency}");

        // Idempotency check - ensure we don't process the same payment multiple times
        // In a real implementation, we would check the idempotency key against a store to see if this request has already been processed
        // can be implemented using a distributed cache or database table to track idempotency keys

        try
        {
            var createAt = DateTime.UtcNow;
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Currency = request.Currency,
                CardNumberMasked = request.CardNumber.MaskCardNumber(),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                CreatedAt = createAt,
                LastUpdatedAt = createAt,
                Status = PaymentStatus.Pending
            };

            await repository.AddAsync(payment);

            var bankResponse = await bankAdapter.AuthorizePaymentAsync(new BankAuthorizationRequest
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
                Cvv = request.Cvv,
                Amount = request.Amount,
                Currency = request.Currency,
            });

            payment.Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
            payment.AuthorizationCode = bankResponse.AuthorizationCode;

            await repository.UpdateAsync(payment.Id, payment);

            logger.LogInformation($"Payment {payment.Id} processed with status {payment.Status}");

            return new PostPaymentResponse
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = payment.CardNumberMasked,
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Currency = payment.Currency,
                Amount = payment.Amount,
                Timestamp = payment.CreatedAt
            };
        }
        catch (BankException ex)
        {
            logger.LogError($"Bank communication error: {ex.Message}");
            throw new PaymentException("Unable to process payment. Please try again later.", ex);
        }
    }
}
