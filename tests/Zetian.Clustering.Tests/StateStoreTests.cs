using Xunit;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Implementation;

namespace Zetian.Clustering.Tests
{
    public class StateStoreTests
    {
        [Fact]
        public async Task InMemoryStateStore_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            InMemoryStateStore store = new();
            string key = "test-key";
            byte[] value = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            await store.SetAsync(key, value);
            byte[]? retrievedValue = await store.GetAsync(key);

            // Assert
            Assert.NotNull(retrievedValue);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task InMemoryStateStore_Delete_RemovesValue()
        {
            // Arrange
            InMemoryStateStore store = new();
            string key = "test-key";
            byte[] value = new byte[] { 1, 2, 3 };

            await store.SetAsync(key, value);

            // Act
            bool deleteResult = await store.DeleteAsync(key);
            byte[]? retrievedValue = await store.GetAsync(key);

            // Assert
            Assert.True(deleteResult);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public async Task InMemoryStateStore_Exists_ReturnsCorrectStatus()
        {
            // Arrange
            InMemoryStateStore store = new();
            string key = "test-key";
            byte[] value = new byte[] { 1, 2, 3 };

            // Act
            bool existsBeforeSet = await store.ExistsAsync(key);
            await store.SetAsync(key, value);
            bool existsAfterSet = await store.ExistsAsync(key);

            // Assert
            Assert.False(existsBeforeSet);
            Assert.True(existsAfterSet);
        }

        [Fact]
        public async Task InMemoryStateStore_TTL_ExpiresValue()
        {
            // Arrange
            InMemoryStateStore store = new();
            string key = "test-key";
            byte[] value = new byte[] { 1, 2, 3 };
            TimeSpan ttl = TimeSpan.FromMilliseconds(100);

            // Act
            await store.SetAsync(key, value, ttl);
            byte[]? immediateValue = await store.GetAsync(key);

            await Task.Delay(150);
            byte[]? expiredValue = await store.GetAsync(key);

            // Assert
            Assert.NotNull(immediateValue);
            Assert.Null(expiredValue);
        }

        [Fact]
        public async Task InMemoryStateStore_IncrementAsync_WorksCorrectly()
        {
            // Arrange
            InMemoryStateStore store = new();
            string key = "counter";

            // Act
            long value1 = await store.IncrementAsync(key, 1);
            long value2 = await store.IncrementAsync(key, 5);
            long value3 = await store.IncrementAsync(key, -2);

            // Assert
            Assert.Equal(1, value1);
            Assert.Equal(6, value2);
            Assert.Equal(4, value3);
        }

        [Fact]
        public async Task InMemoryStateStore_AcquireLock_PreventsDoubleAcquisition()
        {
            // Arrange
            InMemoryStateStore store = new();
            string resource = "test-resource";
            TimeSpan ttl = TimeSpan.FromSeconds(5);

            // Act
            IDistributedLock? lock1 = await store.AcquireLockAsync(resource, ttl);
            IDistributedLock? lock2 = await store.AcquireLockAsync(resource, ttl);

            // Assert
            Assert.NotNull(lock1);
            Assert.Null(lock2);

            // Cleanup
            if (lock1 != null)
            {
                await lock1.ReleaseAsync();
            }
        }
    }
}