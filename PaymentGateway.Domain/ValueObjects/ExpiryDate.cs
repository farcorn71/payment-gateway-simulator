

using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.ValueObjects
{
    public class ExpiryDate
    {
        public int Month { get; }
        public int Year { get; }

        private ExpiryDate(int month, int year)
        {
            Month = month;
            Year = year;
        }

        public static Result<ExpiryDate> Create(int month, int year)
        {
            if (month < 1 || month > 12)
                return Result<ExpiryDate>.Failure(
                    Error.Validation("expiry_month.invalid", "Expiry month must be between 1 and 12"));

            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            if (year < currentYear)
                return Result<ExpiryDate>.Failure(
                    Error.Validation("expiry_year.expired", "Card has expired"));

            if (year == currentYear && month < currentMonth)
                return Result<ExpiryDate>.Failure(
                    Error.Validation("expiry_date.expired", "Card has expired"));

            if (year > currentYear + 20)
                return Result<ExpiryDate>.Failure(
                    Error.Validation("expiry_year.invalid", "Expiry year is too far in the future"));

            return Result<ExpiryDate>.Success(new ExpiryDate(month, year));
        }

        public string ToFormattedString() => $"{Month:D2}/{Year}";

        public override string ToString() => ToFormattedString();
    }
}
