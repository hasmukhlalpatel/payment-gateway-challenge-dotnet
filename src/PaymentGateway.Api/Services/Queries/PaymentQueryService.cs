using PaymentGateway.Api.Models.Queries;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Services.Queries;

public interface IPaymentQueryService
{
    Task<PaymentDetailsResponse> GetStatusAsync(Guid paymentId);
}

public class PaymentQueryService(IPaymentsRepository repository, ILogger<PaymentQueryService> logger)
    : IPaymentQueryService
{
    public async Task<PaymentDetailsResponse> GetStatusAsync(Guid paymentId)
    {
        var payment = await repository.GetAsync(paymentId);

        if (payment == null)
        {
            logger.LogWarning($"Payment {paymentId} not found");
            throw new PaymentException($"Payment {paymentId} not found");
        }

        return new PaymentDetailsResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CardNumberLastFour = payment.CardNumberMasked,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Timestamp = payment.CreatedAt
        };
    }
}
