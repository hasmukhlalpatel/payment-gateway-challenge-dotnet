namespace PaymentGateway.Core.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 100,
        Declined = 900,
        Rejected =999,
    }
}