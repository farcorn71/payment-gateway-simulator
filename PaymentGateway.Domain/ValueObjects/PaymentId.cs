using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGateway.Domain.ValueObjects
{
    public readonly record struct PaymentId
    {
        public Guid Value { get; }

        private PaymentId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Payment ID cannot be empty", nameof(value));

            Value = value;
        }

        public static PaymentId New() => new(Guid.NewGuid());
        public static PaymentId From(Guid value) => new(value);

        public static bool TryParse(string value, out PaymentId paymentId)
        {
            if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
            {
                paymentId = new PaymentId(guid);
                return true;
            }

            paymentId = default;
            return false;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator Guid(PaymentId paymentId) => paymentId.Value;
    }

}
