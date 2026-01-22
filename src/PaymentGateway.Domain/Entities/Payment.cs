using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Entities
{
    public class Payment
    {
        public PaymentId Id { get; private set; }
        public CardNumber CardNumber { get; private set; }
        public ExpiryDate ExpiryDate { get; private set; }
        public Money Money { get; private set; }
        public PaymentStatus Status { get; private set; }
        public string? AuthorizationCode { get; private set; }
        public DateTime CreatedAt { get; private set; }


        private Payment(
            PaymentId id,
            CardNumber cardNumber,
            ExpiryDate expiryDate,
            Money money,
            PaymentStatus status,
            string? authorizationCode)
        {
            Id = id;
            CardNumber = cardNumber;
            ExpiryDate = expiryDate;
            Money = money;
            Status = status;
            AuthorizationCode = authorizationCode;
            CreatedAt = DateTime.UtcNow;
        }

        public static Payment CreateAuthorized(
            PaymentId id,
            CardNumber cardNumber,
            ExpiryDate expiryDate,
            Money money,
            string authorizationCode)
        {
            return new Payment(id, cardNumber, expiryDate, money, PaymentStatus.Authorized, authorizationCode);
        }

        public static Payment CreateDeclined(
            PaymentId id,
            CardNumber cardNumber,
            ExpiryDate expiryDate,
            Money money)
        {
            return new Payment(id, cardNumber, expiryDate, money, PaymentStatus.Declined, null);
        }

        public static Payment CreateRejected(
            PaymentId id,
            CardNumber cardNumber,
            ExpiryDate expiryDate,
            Money money)
        {
            return new Payment(id, cardNumber, expiryDate, money, PaymentStatus.Rejected, null);
        }
    }
}
