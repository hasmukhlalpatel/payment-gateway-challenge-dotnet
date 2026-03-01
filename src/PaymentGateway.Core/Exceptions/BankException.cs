using System;

namespace PaymentGateway.Core.Exceptions
{
    public  class BankException : Exception
    {
        public BankException(string message) : base(message)
        {
        }
        public BankException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}