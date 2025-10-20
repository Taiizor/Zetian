using FluentAssertions;
using System.Net;
using Xunit;
using Zetian.Extensions.RateLimiting;

namespace Zetian.Tests
{
    public class RateLimiterTests
    {
        [Fact]
        public async Task IsAllowedAsync_UnderLimit_ShouldReturnTrue()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Act
            bool result = await limiter.IsAllowedAsync(key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAllowedAsync_OverLimit_ShouldReturnFalse()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(2);
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Act - Record requests up to limit
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);

            // Check if another request is allowed
            bool result = await limiter.IsAllowedAsync(key);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task RecordRequestAsync_ShouldTrackRequests()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Act
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);
            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(3);
        }

        [Fact]
        public async Task GetRemainingAsync_NoRequests_ShouldReturnMax()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(10);
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Act
            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(10);
        }

        [Fact]
        public async Task ResetAsync_ShouldClearRequests()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Record some requests
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);

            // Act
            await limiter.ResetAsync(key);
            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(5);
        }

        [Fact]
        public async Task IsAllowedAsync_DifferentKeys_ShouldBeIndependent()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(1);
            using InMemoryRateLimiter limiter = new(config);
            string key1 = "key1";
            string key2 = "key2";

            // Act
            await limiter.RecordRequestAsync(key1);
            bool result1 = await limiter.IsAllowedAsync(key1);
            bool result2 = await limiter.IsAllowedAsync(key2);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeTrue();
        }

        [Fact]
        public async Task IsAllowedAsync_WithIPAddress_ShouldWork()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerHour(100);
            using InMemoryRateLimiter limiter = new(config);
            IPAddress ipAddress = IPAddress.Parse("192.168.1.1");

            // Act
            bool result = await limiter.IsAllowedAsync(ipAddress);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task RecordRequestAsync_WithIPAddress_ShouldWork()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerHour(100);
            using InMemoryRateLimiter limiter = new(config);
            IPAddress ipAddress = IPAddress.Parse("10.0.0.1");

            // Act
            await limiter.RecordRequestAsync(ipAddress);
            int remaining = await limiter.GetRemainingAsync(ipAddress.ToString());

            // Assert
            remaining.Should().Be(99);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task IsAllowedAsync_InvalidKey_ShouldThrow(string key)
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            using InMemoryRateLimiter limiter = new(config);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => limiter.IsAllowedAsync(key));
        }

        [Fact]
        public async Task IsAllowedAsync_NullIPAddress_ShouldThrow()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            using InMemoryRateLimiter limiter = new(config);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => limiter.IsAllowedAsync((IPAddress)null!));
        }

        [Fact]
        public void RateLimitConfiguration_PerMinute_ShouldSetCorrectly()
        {
            // Act
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(60);

            // Assert
            config.MaxRequests.Should().Be(60);
            config.Window.Should().Be(TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void RateLimitConfiguration_PerHour_ShouldSetCorrectly()
        {
            // Act
            RateLimitConfiguration config = RateLimitConfiguration.PerHour(1000);

            // Assert
            config.MaxRequests.Should().Be(1000);
            config.Window.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void RateLimitConfiguration_PerDay_ShouldSetCorrectly()
        {
            // Act
            RateLimitConfiguration config = RateLimitConfiguration.PerDay(10000);

            // Assert
            config.MaxRequests.Should().Be(10000);
            config.Window.Should().Be(TimeSpan.FromDays(1));
        }

        [Fact]
        public async Task SlidingWindow_ShouldWorkCorrectly()
        {
            // Arrange
            RateLimitConfiguration config = new()
            {
                MaxRequests = 3,
                Window = TimeSpan.FromSeconds(5),
                UseSlidingWindow = true
            };
            using InMemoryRateLimiter limiter = new(config);
            string key = "test-key";

            // Act
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);

            bool result1 = await limiter.IsAllowedAsync(key);

            // Wait for window to slide
            await Task.Delay(TimeSpan.FromSeconds(5.5));
            bool result2 = await limiter.IsAllowedAsync(key);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeTrue();
        }
    }
}