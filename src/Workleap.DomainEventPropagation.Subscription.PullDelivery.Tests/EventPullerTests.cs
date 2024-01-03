using Azure.Messaging.EventGrid.Namespaces;
using FakeItEasy;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class EventPullerTests
{
    [Fact]
    public async Task GivenPullerWithTwoSubscribers_AfterStarting_EveryRegisteredClientWasCalled()
    {
        // Given
        const string sub1 = "Subscriber#1";
        const string sub2 = "Subscriber#2";
        var eventGridClientDescriptors = new EventGridClientDescriptor[] { new(sub1), new(sub2) };

        var clientFactory = A.Fake<IAzureClientFactory<EventGridClient>>();
        var fakeClient1 = A.Fake<EventGridClient>();
        var fakeClient2 = A.Fake<EventGridClient>();
        A.CallTo(() => clientFactory.CreateClient(sub1)).Returns(fakeClient1);
        A.CallTo(() => clientFactory.CreateClient(sub2)).Returns(fakeClient2);

        var optionMonitor = A.Fake<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
        var options1 = new EventPropagationSubscriptionOptions() { TopicName = "topic1", SubscriptionName = "sub1" };
        var options2 = new EventPropagationSubscriptionOptions() { TopicName = "topic2", SubscriptionName = "sub2" };
        A.CallTo(() => optionMonitor.Get(sub1)).Returns(options1);
        A.CallTo(() => optionMonitor.Get(sub2)).Returns(options2);

        // When
        var puller = new EventPuller(eventGridClientDescriptors, clientFactory, optionMonitor);

// We need this to start on the thread pool otherwise it will just block the test
#pragma warning disable CS4014
        Task.Run(() => puller.StartAsync(CancellationToken.None));
#pragma warning restore CS4014
        await puller.StopAsync(CancellationToken.None);

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
}