using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Repositories;

public interface IPaymentsRepository
{
    Task AddAsync(Payment payment);
    Task UpdateAsync(Guid id, Payment payment);

    Task<Payment?> GetAsync(Guid id);
}

//Note: trying to make this repository as simple / generic as possible for demonstration purposes. In a real application, you would likely use a database and an ORM like Dapper or Entity Framework Core.
public class PaymentsRepository : IPaymentsRepository
{
    public List<Payment> Payments = new()
    {
        // Seed with a sample payment for testing purposes. with a fixed ID for easier retrieval in PaymentGateway.Api.http. 
        new Payment
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Amount = 1000,
            Currency = "USD",
            CardNumberMasked = "**** **** **** 1234",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            LastUpdatedAt = DateTime.UtcNow.AddMinutes(-10),
            Status = Core.Enums.PaymentStatus.Authorized,
            AuthorizationCode = "AUTH123456"
        }
    };

    public Task AddAsync(Payment payment)
    {
        Payments.Add(payment);
        return Task.CompletedTask;
    }

    // Convenience overload used by tests to add a response DTO directly.
    public Task AddAsync(PostPaymentResponse response)
    {
        var payment = new Payment
        {
            Id = response.Id,
            Amount = response.Amount,
            Currency = response.Currency,
            CardNumberMasked = response.CardNumberLastFour,
            ExpiryMonth = response.ExpiryMonth,
            ExpiryYear = response.ExpiryYear,
            CreatedAt = response.Timestamp,
            LastUpdatedAt = response.Timestamp,
            Status = response.Status
        };

        Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Guid id, Payment payment)
    {
        var paymentToUpdate =Payments.FirstOrDefault(p => p.Id == id);
        if (paymentToUpdate == null)
            throw new KeyNotFoundException($"Payment with id {id} not found."); // Consider custom exception for better error handling

        paymentToUpdate.Status = payment.Status;
        paymentToUpdate.LastUpdatedAt = DateTime.UtcNow;
        paymentToUpdate.AuthorizationCode = payment.AuthorizationCode;
        return Task.CompletedTask;
    }

    public Task<Payment?> GetAsync(Guid id)
    {
        return Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
    }
}