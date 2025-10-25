using FluentAssertions;
using Xunit;
using Zetian.Protocol;

namespace Zetian.Tests
{
    public class SmtpCommandTests
    {
        [Fact]
        public void Parse_ValidHeloCommand_ShouldParseCorrectly()
        {
            // Arrange
            string commandLine = "HELO client.example.com";

            // Act
            SmtpCommand command = SmtpCommand.Parse(commandLine);

            // Assert
            command.Should().NotBeNull();
            command.Verb.Should().Be("HELO");
            command.Argument.Should().Be("client.example.com");
            command.IsHelo.Should().BeTrue();
        }

        [Fact]
        public void Parse_ValidMailFromCommand_ShouldParseCorrectly()
        {
            // Arrange
            string commandLine = "MAIL FROM:<user@example.com> SIZE=1024";

            // Act
            SmtpCommand command = SmtpCommand.Parse(commandLine);

            // Assert
            command.Should().NotBeNull();
            command.Verb.Should().Be("MAIL");
            command.Argument.Should().Be("FROM:<user@example.com> SIZE=1024");
            command.IsMail.Should().BeTrue();
            command.Parameters.Should().ContainKey("SIZE");
            command.Parameters["SIZE"].Should().Be("1024");
        }

        [Fact]
        public void Parse_CommandWithoutArgument_ShouldParseCorrectly()
        {
            // Arrange
            string commandLine = "QUIT";

            // Act
            SmtpCommand command = SmtpCommand.Parse(commandLine);

            // Assert
            command.Should().NotBeNull();
            command.Verb.Should().Be("QUIT");
            command.Argument.Should().BeNull();
            command.IsQuit.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Parse_InvalidCommandLine_ShouldThrowException(string commandLine)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SmtpCommand.Parse(commandLine));
        }

        [Fact]
        public void TryParse_ValidCommand_ShouldReturnTrue()
        {
            // Arrange
            string commandLine = "EHLO server.example.com";

            // Act
            bool result = SmtpCommand.TryParse(commandLine, out SmtpCommand? command);

            // Assert
            result.Should().BeTrue();
            command.Should().NotBeNull();
            command!.Verb.Should().Be("EHLO");
            command.IsEhlo.Should().BeTrue();
        }

        [Fact]
        public void TryParse_ValidMailCommand_WithParameters_ShouldParse()
        {
            // Act
            bool result = SmtpCommand.TryParse("MAIL FROM:<sender@example.com> SIZE=1234", out SmtpCommand? command);

            // Assert
            result.Should().BeTrue();
            command.Should().NotBeNull();
            command!.Verb.Should().Be("MAIL");
            command.Argument.Should().Be("FROM:<sender@example.com> SIZE=1234");
            command.IsMail.Should().BeTrue();
            command.Parameters.Should().ContainKey("SIZE");
            command.Parameters["SIZE"].Should().Be("1234");
        }

        [Fact]
        public void TryParse_ValidEhloCommand_ShouldParse()
        {
            // Act
            bool result = SmtpCommand.TryParse("EHLO example.com", out SmtpCommand? command);

            // Assert
            result.Should().BeTrue();
            command.Should().NotBeNull();
            command!.Verb.Should().Be("EHLO");
            command.Argument.Should().Be("example.com");
            command.IsEhlo.Should().BeTrue();
        }

        [Fact]
        public void TryParse_InvalidCommand_ShouldReturnFalse()
        {
            // Act
            bool result = SmtpCommand.TryParse("", out SmtpCommand? command);

            // Assert
            result.Should().BeFalse();
            command.Should().BeNull();
        }

        [Fact]
        public void ToString_ShouldReturnOriginalCommand()
        {
            // Arrange
            SmtpCommand command = new("DATA");

            // Act
            string result = command.ToString();

            // Assert
            result.Should().Be("DATA");
        }

        [Fact]
        public void ToString_WithArgument_ShouldReturnFullCommand()
        {
            // Arrange
            SmtpCommand command = new("RCPT", "TO:<recipient@example.com>");

            // Act
            string result = command.ToString();

            // Assert
            result.Should().Be("RCPT TO:<recipient@example.com>");
        }

        [Theory]
        [InlineData("helo", "HELO")]
        [InlineData("EHLO", "EHLO")]
        [InlineData("mail", "MAIL")]
        [InlineData("RcPt", "RCPT")]
        public void Constructor_ShouldNormalizeVerbToUpperCase(string input, string expected)
        {
            // Act
            SmtpCommand command = new(input);

            // Assert
            command.Verb.Should().Be(expected);
        }
    }
}