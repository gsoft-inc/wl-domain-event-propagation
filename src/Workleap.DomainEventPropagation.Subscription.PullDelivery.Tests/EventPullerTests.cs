using AutoBogus;
using Azure.Messaging;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

// Test class cannot be made static
#pragma warning disable CA1052
public class EventPullerTests
#pragma warning restore CA1052
{
    private static IEventGridClientAdapter GivenFakeClient(IEventGridClientWrapperFactory clientWrapperFactory, string subName)
    {
        var fakeClient = A.Fake<IEventGridClientAdapter>();
        A.CallTo(() => clientWrapperFactory.CreateClient(subName)).Returns(fakeClient);
        return fakeClient;
    }

    private static IServiceScopeFactory GivenScopeFactory(ICloudEventHandler handler)
    {
        var scopeFactory = A.Fake<IServiceScopeFactory>();
        var scope = A.Fake<IServiceScope>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ICloudEventHandler>(_ => handler);
        A.CallTo(() => scopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(serviceCollection.BuildServiceProvider());
        return scopeFactory;
    }

    private static EventPropagationSubscriptionOptions GivenEventPropagationSubscriptionOptions(IOptionsMonitor<EventPropagationSubscriptionOptions> optionMonitor, string clientName)
    {
        var option = AutoFaker.Generate<EventPropagationSubscriptionOptions>();
        A.CallTo(() => optionMonitor.Get(clientName)).Returns(option);
        return option;
    }

    private static void RegisterCloudEventsInClient(IEventGridClientAdapter client, EventPropagationSubscriptionOptions option, params EventGridClientAdapter.EventGridClientAdapter.EventBundle[] events)
    {
        A.CallTo(() => client.ReceiveCloudEventsAsync(
            option.TopicName,
            option.SubscriptionName,
            A<CancellationToken>._)).Returns(events);
    }

    private static async Task StartWaitAndStop(IHostedService puller)
    {
// We need this to start on the thread pool otherwise it will just block the test
#pragma warning disable CS4014
        Task.Run(() => puller.StartAsync(CancellationToken.None));
        await Task.Delay(50);
#pragma warning restore CS4014
        await puller.StopAsync(CancellationToken.None);
    }

    public class TwoSubscribers : EventPullerTests
    {
        [Fact]
        public async Task GivenPuller_WhenStarted_ThenEveryRegisteredClientWasCalled()
        {
            // Given
            var clientName1 = AutoFaker.Generate<string>();
            var clientName2 = AutoFaker.Generate<string>();
            var scopeFactory = GivenScopeFactory(A.Fake<ICloudEventHandler>());
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName1), new(clientName2) };

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            var fakeClient1 = GivenFakeClient(clientFactory, clientName1);
            var fakeClient2 = GivenFakeClient(clientFactory, clientName2);

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            var options1 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName1);
            var options2 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName2);

            // When
            using var puller = new EventPuller(scopeFactory, eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPuller>());
            await StartWaitAndStop(puller);

            // Then
            A.CallTo(() => fakeClient1
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeClient2
                    .ReceiveCloudEventsAsync(
                        options2.TopicName,
                        options2.SubscriptionName,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => scopeFactory.CreateScope()).MustHaveHappenedTwiceOrMore();
        }

        [Fact]
        public async Task GivenFailingClient_WhenErrorOccured_ThenKeepsPollingAndDoesNotInterfereWithOtherClients()
        {
            // Given
            var clientName1 = AutoFaker.Generate<string>();
            var clientName2 = AutoFaker.Generate<string>();
            var eventHandler = A.Fake<ICloudEventHandler>();
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName1), new(clientName2) };

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            var failingClient = GivenFakeClient(clientFactory, clientName1);
            A.CallTo(() => failingClient.ReceiveCloudEventsAsync(
                A<string>._,
                A<string>._,
                A<CancellationToken>._)).Throws<Exception>();
            var functionalClient = GivenFakeClient(clientFactory, clientName2);

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            var options1 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName1);
            var options2 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName2);

            var eventBundle = AutoFaker.Generate<EventGridClientAdapter.EventGridClientAdapter.EventBundle>();
            RegisterCloudEventsInClient(functionalClient, options2, eventBundle);

            // When
            using var puller = new EventPuller(GivenScopeFactory(eventHandler), eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPuller>());
            await StartWaitAndStop(puller);

            // Then
            A.CallTo(() => failingClient
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => eventHandler.HandleCloudEventAsync(eventBundle.Event, A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }
    }

    public class OneSubscriber : EventPullerTests, IDisposable
    {
        private readonly EventPuller _puller;
        private readonly IEventGridClientAdapter _client;
        private readonly EventPropagationSubscriptionOptions _option;
        private readonly ICloudEventHandler _eventHandler;
        private readonly EventGridClientAdapter.EventGridClientAdapter.EventBundle _eventBundle1;

        private readonly EventGridClientAdapter.EventGridClientAdapter.EventBundle _eventBundle2;

        public OneSubscriber()
        {
            var clientName = AutoFaker.Generate<string>();
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName) };

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            this._option = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName);

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            this._client = GivenFakeClient(clientFactory, clientName);
            this._eventBundle1 = AutoFaker.Generate<EventGridClientAdapter.EventGridClientAdapter.EventBundle>();
            this._eventBundle2 = AutoFaker.Generate<EventGridClientAdapter.EventGridClientAdapter.EventBundle>();
            RegisterCloudEventsInClient(this._client, this._option, this._eventBundle1, this._eventBundle2);

            this._eventHandler = A.Fake<ICloudEventHandler>(opt => opt.Strict());

            this._puller = new EventPuller(GivenScopeFactory(this._eventHandler), eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPuller>());
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._puller.Dispose();
            }
        }

        [Fact]
        public async Task GivenPuller_WhenMultipleEventsAreReceived_ThenEveryEventsAreHandled()
        {
            // Given
            var call1 = A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle1.Event, A<CancellationToken>._));
            call1.Returns(EventProcessingStatus.Handled);
            var call2 = A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle2.Event, A<CancellationToken>._));
            call2.Returns(EventProcessingStatus.Handled);

            // When
            await StartWaitAndStop(this._puller);

            // Then
            call1.MustHaveHappenedOnceOrMore();
            call2.MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPuller_WhenHandlerReturnsHandledStatus_ThenEventAreAcknowledged()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(EventProcessingStatus.Handled);

            // When
            await StartWaitAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.AcknowledgeCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle1.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.AcknowledgeCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle2.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPullerWithOneSub_WhenHandlerReturnsReleaseStatus_ThenEventAreReleased()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(EventProcessingStatus.Released);

            // When
            await StartWaitAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.ReleaseCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle1.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.ReleaseCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle2.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPullerWithOneSub_WhenHandlerReturnsRejectedStatus_ThenEventAreRejected()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(EventProcessingStatus.Rejected);

            // When
            await StartWaitAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.RejectCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle1.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.RejectCloudEventAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                this._eventBundle2.LockToken,
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }
    }
}