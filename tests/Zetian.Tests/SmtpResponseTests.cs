using FluentAssertions;
using Xunit;
using Zetian.Protocol;

namespace Zetian.Tests
{
    public class SmtpResponseTests
    {
        [Fact]
        public void Constructor_ValidCode_ShouldCreateResponse()
        {
            // Act
            SmtpResponse response = new(250, "OK");

            // Assert
            response.Code.Should().Be(250);
            response.Message.Should().Be("OK");
            response.IsPositive.Should().BeTrue();
            response.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(99)]
        [InlineData(600)]
        [InlineData(1000)]
        public void Constructor_InvalidCode_ShouldThrowException(int code)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new SmtpResponse(code, "Test"));
        }

        [Fact]
        public void Constructor_MultipleLines_ShouldStoreAllLines()
        {
            // Arrange
            string[] lines = new[] { "First line", "Second line", "Third line" };

            // Act
            SmtpResponse response = new(250, lines);

            // Assert
            response.Lines.Count.Should().Be(3);
            response.Lines.Should().ContainInOrder(lines);
            response.Message.Should().Be("First line");
        }

        [Theory]
        [InlineData(200, 299, true, false, false, false)]
        [InlineData(300, 399, false, true, false, false)]
        [InlineData(400, 499, false, false, true, false)]
        [InlineData(500, 599, false, false, false, true)]
        public void ResponseCategories_ShouldBeCorrect(
            int codeMin, int codeMax,
            bool isPositive, bool isPositiveIntermediate,
            bool isTransientNegative, bool isPermanentNegative)
        {
            // Arrange
            int code = (codeMin + codeMax) / 2;

            // Act
            SmtpResponse response = new(code, "Test");

            // Assert
            response.IsPositive.Should().Be(isPositive);
            response.IsPositiveIntermediate.Should().Be(isPositiveIntermediate);
            response.IsTransientNegative.Should().Be(isTransientNegative);
            response.IsPermanentNegative.Should().Be(isPermanentNegative);
        }

        [Fact]
        public void IsSuccess_PositiveCodes_ShouldReturnTrue()
        {
            // Arrange
            SmtpResponse response1 = new(250, "OK");
            SmtpResponse response2 = new(354, "Start mail input");

            // Act & Assert
            response1.IsSuccess.Should().BeTrue();
            response2.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void IsError_NegativeCodes_ShouldReturnTrue()
        {
            // Arrange
            SmtpResponse response1 = new(450, "Mailbox unavailable");
            SmtpResponse response2 = new(550, "User not found");

            // Act & Assert
            response1.IsError.Should().BeTrue();
            response2.IsError.Should().BeTrue();
        }

        [Fact]
        public void ToString_SingleLine_ShouldFormatCorrectly()
        {
            // Arrange
            SmtpResponse response = new(250, "Message accepted");

            // Act
            string result = response.ToString();

            // Assert
            result.Should().Be("250 Message accepted\r\n");
        }

        [Fact]
        public void ToString_MultipleLines_ShouldFormatWithDashes()
        {
            // Arrange
            SmtpResponse response = new(250, "First", "Second", "Third");

            // Act
            string result = response.ToString();

            // Assert
            string[] lines = result.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(3);
            lines[0].Should().Be("250-First");
            lines[1].Should().Be("250-Second");
            lines[2].Should().Be("250 Third");
        }

        [Fact]
        public void CommonResponses_ShouldHaveCorrectCodes()
        {
            // Assert
            SmtpResponse.Ok.Code.Should().Be(250);
            SmtpResponse.ServiceReady.Code.Should().Be(220);
            SmtpResponse.ServiceClosing.Code.Should().Be(221);
            SmtpResponse.StartMailInput.Code.Should().Be(354);
            SmtpResponse.AuthenticationRequired.Code.Should().Be(530);
            SmtpResponse.AuthenticationFailed.Code.Should().Be(535);
            SmtpResponse.SyntaxError.Code.Should().Be(500);
            SmtpResponse.BadSequence.Code.Should().Be(503);
            SmtpResponse.TransactionFailed.Code.Should().Be(554);
        }

        [Fact]
        public void Constructor_EmptyMessage_ShouldUseDefaultMessage()
        {
            // Act
            SmtpResponse response = new(250, "");

            // Assert
            response.Lines.Should().HaveCount(1);
            response.Message.Should().Be("OK");
        }

        [Fact]
        public void Constructor_NullLines_ShouldUseDefaultMessage()
        {
            // Act
            SmtpResponse response = new(421, (string[])null!);

            // Assert
            response.Lines.Should().HaveCount(1);
            response.Message.Should().Be("Service not available, closing transmission channel");
        }

        [Fact]
        public void ToString_NoLines_ShouldUseDefaultMessage()
        {
            // Arrange
            SmtpResponse response = new(250, new string[] { });

            // Act
            string result = response.ToString();

            // Assert
            result.Should().Be("250 OK\r\n");
        }
    }
}