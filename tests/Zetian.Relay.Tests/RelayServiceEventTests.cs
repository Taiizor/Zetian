using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Enums;
using Zetian.Relay.Models;
using Zetian.Relay.Models.EventArgs;
using Zetian.Relay.Queue;
using Zetian.Relay.Services;

namespace Zetian.Relay.Tests
{
    /// <summary>
    /// Tests for the delivery lifecycle events exposed by <see cref="RelayService"/>:
    /// MessageDelivered, MessageBounced, MessageDeferred and MessageExpired.
    /// </summary>
    public class RelayServiceEventTests
    {
        private const string Recipient = "recipient@example.com";
        private const string SmartHost = "smtp.test:25";

        private static Mock<ISmtpMessage> CreateMessage(
            string from = "sender@example.com",
            string to = Recipient)
        {
            Mock<ISmtpMessage> message = new();
            message.SetupGet(m => m.Id).Returns(Guid.NewGuid().ToString("N"));
            message.SetupGet(m => m.From).Returns(new MailAddress(from));
            message.SetupGet(m => m.Recipients).Returns(new List<MailAddress> { new(to) });
            message.SetupGet(m => m.Subject).Returns("Test message");
            message.SetupGet(m => m.Headers).Returns(new Dictionary<string, string>());
            return message;
        }

        private static RelayConfiguration CreateConfiguration(
            int maxRetries = 10,
            bool enableBounceMessages = false)
        {
            return new RelayConfiguration
            {
                DefaultSmartHost = new SmartHostConfiguration { Host = "smtp.test", Port = 25 },
                MaxRetryCount = maxRetries,
                EnableBounceMessages = enableBounceMessages,
                RequireAuthentication = false
            };
        }

        private static Mock<ISmtpClient> CreateClient(
            Func<SmtpDeliveryResult>? sendResult = null,
            Exception? sendThrows = null)
        {
            Mock<ISmtpClient> client = new();
            client.SetupGet(c => c.IsConnected).Returns(true);
            client.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            client.Setup(c => c.AuthenticateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            client.Setup(c => c.SendRawAsync(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SmtpDeliveryResult.CreateSuccess(Array.Empty<string>()));

            var send = client.Setup(c => c.SendAsync(It.IsAny<ISmtpMessage>(), It.IsAny<CancellationToken>()));
            if (sendThrows != null)
            {
                send.ThrowsAsync(sendThrows);
            }
            else
            {
                send.ReturnsAsync(() => sendResult!());
            }

            return client;
        }

        private static RelayService CreateService(
            RelayConfiguration configuration,
            InMemoryRelayQueue queue,
            Mock<ISmtpClient> client)
        {
            return new RelayService(configuration, queue, logger: null, clientFactory: _ => client.Object);
        }

        [Fact]
        public async Task MessageDelivered_IsRaised_OnSuccessfulDelivery()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateSuccess(new[] { Recipient }));
            RelayService service = CreateService(CreateConfiguration(), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageDelivered += (_, e) => raised = e;
            service.MessageBounced += (_, e) => throw new InvalidOperationException("Bounce should not fire");

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal(message.QueueId, raised!.QueueId);
            Assert.Equal(RelayStatus.Delivered, raised.Status);
            Assert.NotNull(raised.Result);
            Assert.True(raised.Result!.Success);
            Assert.Null(raised.Error);
        }

        [Fact]
        public async Task MessageBounced_IsRaised_OnPermanentFailure()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateFailure("Mailbox unavailable", 550));
            RelayService service = CreateService(CreateConfiguration(), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageBounced += (_, e) => raised = e;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal(message.QueueId, raised!.QueueId);
            Assert.Equal(RelayStatus.Failed, raised.Status);
            Assert.Equal("Mailbox unavailable", raised.Error);
            Assert.NotNull(raised.Result);
            Assert.False(raised.Result!.Success);
        }

        [Fact]
        public async Task MessageBounced_IsRaised_WhenMaxRetriesReached_OnTemporaryFailure()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateFailure("Try again later", 451, isTemporary: true));
            RelayService service = CreateService(CreateConfiguration(maxRetries: 0), queue, client);

            RelayDeliveryEventArgs? bounced = null;
            RelayDeliveryEventArgs? deferred = null;
            service.MessageBounced += (_, e) => bounced = e;
            service.MessageDeferred += (_, e) => deferred = e;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(bounced);
            Assert.Null(deferred);
            Assert.Equal(RelayStatus.Failed, bounced!.Status);
        }

        [Fact]
        public async Task MessageBounced_IsRaised_EvenWhenBounceMessagesDisabled()
        {
            // EnableBounceMessages defaults to false in CreateConfiguration: the event must
            // still fire so callers can log failures regardless of NDR generation.
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateFailure("Rejected", 550));
            RelayService service = CreateService(
                CreateConfiguration(enableBounceMessages: false), queue, client);

            bool raised = false;
            service.MessageBounced += (_, _) => raised = true;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.True(raised);
        }

        [Fact]
        public async Task MessageBounced_IsRaised_WhenBounceMessagesEnabled()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateFailure("Rejected", 550));
            RelayService service = CreateService(
                CreateConfiguration(enableBounceMessages: true), queue, client);

            bool raised = false;
            service.MessageBounced += (_, _) => raised = true;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.True(raised);
        }

        [Fact]
        public async Task MessageDeferred_IsRaised_OnTemporaryFailure_WithRetriesRemaining()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateFailure("Greylisted", 451, isTemporary: true));
            RelayService service = CreateService(CreateConfiguration(maxRetries: 10), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageDeferred += (_, e) => raised = e;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal(message.QueueId, raised!.QueueId);
            Assert.Equal(RelayStatus.Deferred, raised.Status);
            Assert.NotNull(raised.NextRetryTime);
            Assert.Equal(1, raised.RetryCount);
        }

        [Fact]
        public async Task MessageDeferred_IsRaised_OnTransportException_WithRetriesRemaining()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(sendThrows: new TimeoutException("Connection timed out"));
            RelayService service = CreateService(CreateConfiguration(maxRetries: 10), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageDeferred += (_, e) => raised = e;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal("Connection timed out", raised!.Error);
            Assert.NotNull(raised.NextRetryTime);
        }

        [Fact]
        public async Task MessageBounced_IsRaised_OnTransportException_WhenNoRetriesLeft()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(sendThrows: new InvalidOperationException("Fatal"));
            RelayService service = CreateService(CreateConfiguration(maxRetries: 0), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageBounced += (_, e) => raised = e;

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);
            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal("Fatal", raised!.Error);
            Assert.Equal(RelayStatus.Failed, raised.Status);
        }

        [Fact]
        public async Task MessageExpired_IsRaised_WhenMessageHasExpired()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateSuccess(new[] { Recipient }));
            RelayService service = CreateService(CreateConfiguration(), queue, client);

            RelayDeliveryEventArgs? raised = null;
            service.MessageExpired += (_, e) => raised = e;
            service.MessageDelivered += (_, _) => throw new InvalidOperationException("Delivery should not fire");

            // Build a message whose lifetime has already elapsed so DeliverMessageAsync
            // takes the expiry branch before any send attempt.
            RelayMessage message = new(CreateMessage().Object, SmartHost, RelayPriority.Normal, TimeSpan.FromMilliseconds(1));
            await Task.Delay(30);

            await service.DeliverMessageAsync(message, CancellationToken.None);

            Assert.NotNull(raised);
            Assert.Equal(message.QueueId, raised!.QueueId);
            Assert.True(raised.Message.IsExpired);
            Assert.Equal("Message expired", raised.Error);
            // The client must never be contacted for an already-expired message.
            client.Verify(c => c.SendAsync(It.IsAny<ISmtpMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EventHandlerException_DoesNotPropagate()
        {
            InMemoryRelayQueue queue = new();
            Mock<ISmtpClient> client = CreateClient(
                () => SmtpDeliveryResult.CreateSuccess(new[] { Recipient }));
            RelayService service = CreateService(CreateConfiguration(), queue, client);

            service.MessageDelivered += (_, _) => throw new InvalidOperationException("handler boom");

            IRelayMessage message = await queue.EnqueueAsync(CreateMessage().Object, SmartHost);

            // A throwing subscriber must be swallowed by the raiser so delivery is unaffected.
            Exception? caught = await Record.ExceptionAsync(
                () => service.DeliverMessageAsync(message, CancellationToken.None));

            Assert.Null(caught);
        }
    }
}