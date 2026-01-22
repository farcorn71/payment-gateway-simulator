using FluentAssertions;

using PaymentGateway.Domain.ValueObjects;

using Xunit;

namespace PaymentGateway.Domain.Tests.ValueObjects
{
    public sealed class CardNumberTests
    {
        [Theory]
        [InlineData("4532015112830366")] // Valid Visa
        [InlineData("5425233430109903")] // Valid Mastercard
        [InlineData("378282246310005")]  // Valid Amex
        public void Create_WithValidCardNumber_ShouldSucceed(string cardNumber)
        {
            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Value.Should().Be(cardNumber);
            result.Value.LastFourDigits.Should().Be(cardNumber[^4..]);
        }

        [Fact]
        public void Create_WithValidCardNumber_ShouldMaskCorrectly()
        {
            // Arrange
            var cardNumber = "4532015112830366";

            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            result.Value!.MaskedValue.Should().Be("************0366");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyCardNumber_ShouldFail(string cardNumber)
        {
            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("card_number.required");
        }

        [Theory]
        [InlineData("123")]           // Too short
        [InlineData("1234567890123")] // Too short (13 digits)
        [InlineData("12345678901234567890")] // Too long (20 digits)
        public void Create_WithInvalidLength_ShouldFail(string cardNumber)
        {
            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("card_number.invalid_length");
        }

        [Theory]
        [InlineData("453201511283036a")]
        [InlineData("4532-0151-1283-0366")]
        [InlineData("4532 0151 1283 0366")]
        public void Create_WithNonNumericCharacters_ShouldHandleSpacesAndDashesButRejectLetters(string cardNumber)
        {
            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            if (cardNumber.Contains('a'))
            {
                result.IsSuccess.Should().BeFalse();
                result.Error!.Code.Should().Be("card_number.invalid_format");
            }
            else
            {
                // Spaces and dashes should be removed
                result.IsSuccess.Should().BeTrue();
            }
        }

        [Theory]
        [InlineData("1234567890123456")] // Invalid Luhn
        [InlineData("4532015112830367")] // Invalid Luhn (last digit wrong)
        public void Create_WithInvalidLuhnChecksum_ShouldFail(string cardNumber)
        {
            // Act
            var result = CardNumber.Create(cardNumber);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error!.Code.Should().Be("card_number.invalid_checksum");
        }

        [Fact]
        public void ToString_ShouldReturnMaskedValue()
        {
            // Arrange
            var cardNumber = "4532015112830366";
            var card = CardNumber.Create(cardNumber).Value!;

            // Act
            var stringValue = card.ToString();

            // Assert
            stringValue.Should().Be("************0366");
        }
    }

}
