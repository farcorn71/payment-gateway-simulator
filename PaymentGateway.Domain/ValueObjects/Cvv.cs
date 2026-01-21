using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PaymentGateway.Domain.ValueObjects
{
    public sealed class Cvv
    {
        public string Value { get; }

        private Cvv(string value)
        {
            Value = value;
        }

        public static Result<Cvv> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.required", "CVV is required"));

            var trimmedValue = value.Trim();

            if (!trimmedValue.All(char.IsDigit))
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.invalid_format", "CVV must contain only digits"));

            if (trimmedValue.Length < 3 || trimmedValue.Length > 4)
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.invalid_length", "CVV must be 3 or 4 digits"));

            return Result<Cvv>.Success(new Cvv(trimmedValue));
        }

        // Never expose actual CVV value in logs or responses
        public override string ToString() => "***";
    }
}
