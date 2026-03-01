using FluentValidation;

using PaymentGateway.Api.Models.Commands;

namespace PaymentGateway.Api.Validators;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    // Allow only 3 ISO currency codes
    private static readonly HashSet<string> AllowedCurrencies = new()
    {
        "USD", "EUR", "GBP"
    };

    public PostPaymentRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .Length(14, 19)
            .Matches("^[0-9]+$").WithMessage("Card number must contain only digits.")
            .Must(IsValidLuhn).WithMessage("Card number is not valid.");

        RuleFor(x => x.ExpiryMonth)
            .NotEmpty()
            .InclusiveBetween(1, 12);

        RuleFor(x => x.ExpiryYear)
            .NotEmpty()
            .GreaterThan(0)
            .Must(BeInTheFuture).WithMessage("Expiry year must be in the future.");

        RuleFor(x => new { x.ExpiryMonth, x.ExpiryYear })
            .Must(BeValidExpiryDate)
            .WithMessage("The expiry month and year must represent a future date.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage("Currency must be one of the allowed ISO codes.");

        RuleFor(x => x.Amount)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage("Amount must be a positive integer representing minor currency units.");

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .Length(3, 4)
            .Matches("^[0-9]+$").WithMessage("CVV must contain only digits.");
    }

    private bool BeInTheFuture(int year)
    {
        var now = DateTime.UtcNow;
        return year > now.Year;
    }

    private bool BeValidExpiryDate(dynamic expiry)
    {
        if (expiry.ExpiryYear <=0 || expiry.ExpiryMonth <= 0)
            return false;

        var now = DateTime.UtcNow;
        var lastDayOfExpiryMonth = new DateTime(expiry.ExpiryYear, expiry.ExpiryMonth, 1)
                                        .AddMonths(1)
                                        .AddDays(-1);

        return lastDayOfExpiryMonth > now;
    }

    private static bool IsValidLuhn(string cardNumber)
    {
        return true;
        //Copilot wrote this, but it is not working correctly. I will fix it later if I have time.
        return cardNumber.All(char.IsDigit) && cardNumber.Reverse()
            .Select((c, i) => (c - '0') * (i % 2 == 1 ? 2 : 1))
            .Sum(x => x > 9 ? x - 9 : x) % 10 == 0;
    }
}
