using PaymentGateway.Domain.ValueObjects;
using FluentAssertions;

namespace PaymentGateway.Domain.Tests.ValueObjects
{
    public class ExpiryDateTests
    {
        [Fact]
        public void Create_WithFutureDate_ShouldSucceed()
        {
            // Arrange
            var futureYear = DateTime.UtcNow.Year + 1;
            var month = 12;

            // Act
            var result = ExpiryDate.Create(month, futureYear);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Month.Should().Be(month);
            result.Value.Year.Should().Be(futureYear);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        [InlineData(100)]
        public void Create_WithInvalidMonth_ShouldFail(int month)
        {
            // Arrange
            var futureYear = DateTime.UtcNow.Year + 1;

            // Act
            var result = ExpiryDate.Create(month, futureYear);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("expiry_month.invalid");
        }

        [Fact]
        public void Create_WithPastYear_ShouldFail()
        {
            // Arrange
            var pastYear = DateTime.UtcNow.Year - 1;
            var month = 6;

            // Act
            var result = ExpiryDate.Create(month, pastYear);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("expiry_year.expired");
        }

        [Fact]
        public void Create_WithCurrentYearButPastMonth_ShouldFail()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var pastMonth = Math.Max(1, now.Month - 1);

            // Skip test if we're in January (no past month in current year)
            if (now.Month == 1)
                return;

            // Act
            var result = ExpiryDate.Create(pastMonth, currentYear);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("expiry_date.expired");
        }

        [Fact]
        public void Create_WithFarFutureYear_ShouldFail()
        {
            // Arrange
            var farFutureYear = DateTime.UtcNow.Year + 25;
            var month = 6;

            // Act
            var result = ExpiryDate.Create(month, farFutureYear);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("expiry_year.invalid");
        }

        [Fact]
        public void ToFormattedString_ShouldFormatCorrectly()
        {
            // Arrange
            var futureYear = DateTime.UtcNow.Year + 1;
            var month = 3;
            var expiryDate = ExpiryDate.Create(month, futureYear).Value!;

            // Act
            var formatted = expiryDate.ToFormattedString();

            // Assert
            formatted.Should().Be($"03/{futureYear}");
        }
    }

}
