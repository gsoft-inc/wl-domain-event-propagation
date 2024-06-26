using AutoBogus;
using Azure.Messaging;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.EventGridClientAdapter;
using Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests.TestExtensions;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public abstract class EventPullerServiceTests
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
        var option = new AutoFaker<EventPropagationSubscriptionOptions>().RuleFor(x => x.MaxDegreeOfParallelism, f => f.Random.Int(min: 0)).Generate();
        A.CallTo(() => optionMonitor.Get(clientName)).Returns(option);
        return option;
    }

    private static void RegisterCloudEventsInClient(IEventGridClientAdapter client, EventPropagationSubscriptionOptions option, params EventGridClientAdapter.EventGridClientAdapter.EventBundle[] events)
    {
        // The first call will return the events, subsequent calls will return an empty array after a delay
        // The actual implementation of EventGrid will perform similarly if there are only some events to return
        A.CallTo(() => client.ReceiveCloudEventsAsync(
            option.TopicName,
            option.SubscriptionName,
            A<int>._,
            A<CancellationToken>._))
            .Returns(events).Once()
            .Then.ReturnsLazily(async x =>
            {
                await Task.Delay(100);
                return [];
            });
    }

    private static async Task StartWaitAndStop(IHostedService pullerService)
    {
        // We need this to start on the thread pool otherwise it will just block the test
        Task.Run(() => pullerService.StartAsync(CancellationToken.None)).Forget();
        await Task.Delay(50);
        await pullerService.StopAsync(CancellationToken.None);
    }

    public class TwoSubscribers : EventPullerServiceTests
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
            using var pullerService = new EventPullerService(scopeFactory, eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPullerService>());
            await StartWaitAndStop(pullerService);

            // Then
            A.CallTo(() => fakeClient1
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<int>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeClient2
                    .ReceiveCloudEventsAsync(
                        options2.TopicName,
                        options2.SubscriptionName,
                        A<int>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
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
                A<int>._,
                A<CancellationToken>._)).Throws<Exception>();
            var functionalClient = GivenFakeClient(clientFactory, clientName2);

            var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
            var options1 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName1);
            var options2 = GivenEventPropagationSubscriptionOptions(optionMonitor, clientName2);

            var eventBundle = AutoFaker.Generate<EventGridClientAdapter.EventGridClientAdapter.EventBundle>();
            RegisterCloudEventsInClient(functionalClient, options2, eventBundle);

            // When
            using var pullerService = new EventPullerService(GivenScopeFactory(eventHandler), eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPullerService>());
            await StartWaitAndStop(pullerService);

            // Then
            A.CallTo(() => failingClient
                    .ReceiveCloudEventsAsync(
                        options1.TopicName,
                        options1.SubscriptionName,
                        A<int>._,
                        A<CancellationToken>._))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => eventHandler.HandleCloudEventAsync(eventBundle.Event, A<CancellationToken>._)).MustHaveHappenedOnceOrMore();
        }
    }

    public class OneSubscriber : EventPullerServiceTests, IDisposable
    {
        private readonly EventPullerService _pullerService;
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

            this._pullerService = new EventPullerService(GivenScopeFactory(this._eventHandler), eventGridClientDescriptors, clientFactory, optionMonitor, new NullLogger<EventPullerService>());
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
                this._pullerService.Dispose();
            }
        }

        [Fact]
        public async Task GivenTwoEventReceived_WhenHandleSuccessfully_ThenEveryEventsAreHandled()
        {
            // Given
            var call1 = A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle1.Event, A<CancellationToken>._));
            var call2 = A.CallTo(() => this._eventHandler.HandleCloudEventAsync(this._eventBundle2.Event, A<CancellationToken>._));
        
            // When
            await StartWaitAndStop(this._pullerService);

            // Then
            call1.MustHaveHappenedOnceOrMore();
            call2.MustHaveHappenedOnceOrMore();
        }

        [Fact]
        public async Task GivenTwoEventsReceived_WhenHandleSuccessfully_ThenEventsAreAcknowledged()
        {
            var acknowledgedEvents = new List<string>();

            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Returns(Task.CompletedTask);

            // Special case: the IEnumerable received by the call needs to be consumed immediately, otherwise the data is never removed from the channel until the end when it is too late
            A.CallTo(() => this._client.AcknowledgeCloudEventsAsync(A<string>._, A<string>._, A<IEnumerable<string>>._, A<CancellationToken>._))
                .Invokes(x => acknowledgedEvents.AddRange(x.GetArgument<IEnumerable<string>>(2)!));

            // When
            await StartWaitAndStop(this._pullerService);

            // Then
            Assert.Contains(this._eventBundle1.LockToken, acknowledgedEvents);
            Assert.Contains(this._eventBundle2.LockToken, acknowledgedEvents);
        }

        [Fact]
        public async Task GivenTwoEventsReceived_WhenHandleThrowUnhandledException_ThenEventsAreReleased()
        {
            var releasedEvents = new List<string>();

            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .ThrowsAsync(new Exception("Unhandled exception"));

            // Special case: the IEnumerable received by the call needs to be consumed immediately, otherwise the data is never removed from the channel until the end when it is too late
            A.CallTo(() => this._client.ReleaseCloudEventsAsync(A<string>._, A<string>._, A<IEnumerable<string>>._, A<CancellationToken>._))
                .Invokes(x => releasedEvents.AddRange(x.GetArgument<IEnumerable<string>>(2)!));

            // When
            await StartWaitAndStop(this._pullerService);

            // Then
            Assert.Contains(this._eventBundle1.LockToken, releasedEvents);
            Assert.Contains(this._eventBundle2.LockToken, releasedEvents);
        }

        [Theory]
        [MemberData(nameof(RejectingExceptions))]
        public async Task GivenTwoEventsReceived_WhenHandleThrowRejectingException_ThenEventsAreRejected(Exception exception)
        {
            var rejectedEvents = new List<string>();

            // Given
            A.CallTo(() => this._eventHandler.HandleCloudEventAsync(A<CloudEvent>._, A<CancellationToken>._))
                .Throws(exception);

            // Special case: the IEnumerable received by the call needs to be consumed immediately, otherwise the data is never removed from the channel until the end when it is too late
            A.CallTo(() => this._client.RejectCloudEventsAsync(A<string>._, A<string>._, A<IEnumerable<string>>._, A<CancellationToken>._))
                .Invokes(x => rejectedEvents.AddRange(x.GetArgument<IEnumerable<string>>(2)!));

            // When
            await StartWaitAndStop(this._pullerService);

            // Then
            Assert.Contains(this._eventBundle1.LockToken, rejectedEvents);
            Assert.Contains(this._eventBundle2.LockToken, rejectedEvents);
        }

        public static IEnumerable<object[]> RejectingExceptions()
        {
            yield return [new DomainEventTypeNotRegisteredException("event")];
            yield return [new CloudEventSerializationException("type", new Exception())];
            yield return [new DomainEventHandlerNotRegisteredException("eventName")];
        }
    }
}
