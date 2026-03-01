using System;

namespace PaymentGateway.Core.Exceptions
{
    public  class PaymentException : Exception
    {
        public PaymentException()
        {
        }
        public PaymentException(string message) : base(message)
        {
        }
        public PaymentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
