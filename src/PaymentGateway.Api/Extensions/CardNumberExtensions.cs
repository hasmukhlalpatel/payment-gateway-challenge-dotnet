namespace PaymentGateway.Api.Extensions;

public static class CardNumberExtensions
{
    public static string MaskCardNumber(this string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return "****"; // Return masked if invalid
        var last4Digits = cardNumber[^4..];
        return $"**** **** **** {last4Digits}";
    }
}