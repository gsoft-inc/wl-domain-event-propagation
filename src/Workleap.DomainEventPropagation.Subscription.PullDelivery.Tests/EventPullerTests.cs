using AutoBogus;
using Azure.Messaging;
using Azure.Messaging.EventGrid.Namespaces;
using FakeItEasy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.ClientWrapper;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class EventPullerTests
{
    private EventGridClientWrapper GivenFakeClient(IEventGridClientWrapperFactory clientWrapperFactory, string subName)
    {
        var fakeClient = A.Fake<EventGridClientWrapper>();
        A.CallTo(() => clientWrapperFactory.CreateClient(subName)).Returns(fakeClient);
        return fakeClient;
    }

    private EventPropagationSubscriptionOptions GivenOption(IOptionsMonitor<EventPropagationSubscriptionOptions> optionMonitor, string clientName)
    {
        var option = AutoFaker.Generate<EventPropagationSubscriptionOptions>();
        A.CallTo(() => optionMonitor.Get(clientName)).Returns(option);
        return option;
    }

    private void RegisterCloudEventsInClient(EventGridClientWrapper client, EventPropagationSubscriptionOptions option, params EventGridClientWrapper.EventBundle[] events)
    {
        A.CallTo(() => client.ReceiveCloudEventsAsync(
            option.TopicName,
            option.SubscriptionName,
            A<int?>._,
            A<TimeSpan?>._,
            A<CancellationToken>._)).Returns(events);
    }

    private async Task StartAndStop(IHostedService puller)
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
        public async Task GivenPuller_AfterStarting_EveryRegisteredClientWasCalled()
        {
            // Given
            var clientName1 = AutoFaker.Generate<string>();
            var clientName2 = AutoFaker.Generate<string>();
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName1), new(clientName2) };

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            var fakeClient1 = this.GivenFakeClient(clientFactory, clientName1);
            var fakeClient2 = this.GivenFakeClient(clientFactory, clientName2);

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            var options1 = this.GivenOption(optionMonitor, clientName1);
            var options2 = this.GivenOption(optionMonitor, clientName2);

            // When
            var puller = new EventPuller(eventGridClientDescriptors, clientFactory, A.Fake<ICloudEventHandler>(), optionMonitor, new NullLogger<EventPuller>());
            await this.StartAndStop(puller);

            // Then
            A.CallTo(() => fakeClient1
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<int?>._,
                        A<TimeSpan?>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeClient2
                    .ReceiveCloudEventsAsync(
                        options2.TopicName,
                        options2.SubscriptionName,
                        A<int?>._,
                        A<TimeSpan?>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenFailingClient_AfterErrorOccured_KeepsPollingAndDoesNotInterfereWithOtherClients()
        {
            // Given
            var clientName1 = AutoFaker.Generate<string>();
            var clientName2 = AutoFaker.Generate<string>();
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName1), new(clientName2) };

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            var failingClient = this.GivenFakeClient(clientFactory, clientName1);
            A.CallTo(() => failingClient.ReceiveCloudEventsAsync(
                A<string>._,
                A<string>._,
                A<int?>._,
                A<TimeSpan?>._,
                A<CancellationToken>._)).Throws<Exception>();
            var functionalClient = this.GivenFakeClient(clientFactory, clientName2);

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            var options1 = this.GivenOption(optionMonitor, clientName1);
            var options2 = this.GivenOption(optionMonitor, clientName2);

            var eventBundle = AutoFaker.Generate<EventGridClientWrapper.EventBundle>();
            this.RegisterCloudEventsInClient(functionalClient, options2, eventBundle);
            var eventHandler = A.Fake<ICloudEventHandler>();

            // When
            var puller = new EventPuller(eventGridClientDescriptors, clientFactory, eventHandler, optionMonitor, new NullLogger<EventPuller>());
            await this.StartAndStop(puller);

            // Then
            A.CallTo(() => failingClient
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<int?>._,
                        A<TimeSpan?>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => eventHandler.HandleCloudEventAsync(eventBundle.Event, A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }
    }

    public class OneSubscriber : EventPullerTests, IDisposable
    {
        private readonly EventPuller _puller;
        private readonly EventGridClientWrapper _client;
        private readonly EventPropagationSubscriptionOptions _option;
        private readonly ICloudEventHandler _eventHandler;
        private readonly EventGridClientWrapper.EventBundle _eventBundle1;

        private readonly EventGridClientWrapper.EventBundle _eventBundle2;

        public OneSubscriber()
        {
            var clientName = AutoFaker.Generate<string>();
            var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(clientName) };

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            this._option = this.GivenOption(optionMonitor, clientName);

            var clientFactory = A.Fake<IEventGridClientWrapperFactory>();
            this._client = this.GivenFakeClient(clientFactory, clientName);
            this._eventBundle1 = AutoFaker.Generate<EventGridClientWrapper.EventBundle>();
            this._eventBundle2 = AutoFaker.Generate<EventGridClientWrapper.EventBundle>();
            this.RegisterCloudEventsInClient(this._client, this._option, this._eventBundle1, this._eventBundle2);

            this._eventHandler = A.Fake<ICloudEventHandler>();

            this._puller = new EventPuller(eventGridClientDescriptors, clientFactory, this._eventHandler, optionMonitor, new NullLogger<EventPuller>());
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
        public async Task GivenPullerWithOneSub_WhenMultipleEventsAreReceived_ThenEveryEventsAreHandled()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(HandlingStatus.Handled);

            // When
            await this.StartAndStop(this._puller);

            // Then
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle1.Event, A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle2.Event, A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPullerWithOneSub_WhenHandlerReturnsHandledStatus_ThenEventAreAcknowledged()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(HandlingStatus.Handled);

            // When
            await this.StartAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.AcknowledgeCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<AcknowledgeOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle1.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.AcknowledgeCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<AcknowledgeOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle2.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPullerWithOneSub_WhenHandlerReturnsReleaseStatus_ThenEventAreReleased()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(HandlingStatus.Released);

            // When
            await this.StartAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.ReleaseCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<ReleaseOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle1.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.ReleaseCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<ReleaseOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle2.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenPullerWithOneSub_WhenHandlerReturnsRejectedStatus_ThenEventAreRejected()
        {
            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(HandlingStatus.Rejected);

            // When
            await this.StartAndStop(this._puller);

            // Then
            A.CallTo(() => this._client.RejectCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<RejectOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle1.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => this._client.RejectCloudEventsAsync(
                this._option.TopicName,
                this._option.SubscriptionName,
                A<RejectOptions>.That.Matches(opt => opt.LockTokens.Single() == this._eventBundle2.LockToken),
                A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }
    }
}