using Azure.Messaging.EventGrid;
using FakeItEasy;
using GSoft.Extensions.Xunit;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests;

public class TracingBehaviorFixture : BaseUnitFixture
{
    public override IServiceCollection ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Fake EventGridPublisherClient
        var publisherClient = A.Fake<EventGridPublisherClient>();
        var clientFactory = A.Fake<IAzureClientFactory<EventGridPublisherClient>>();
        A.CallTo(() => clientFactory.CreateClient(EventPropagationPublisherOptions.ClientName)).Returns(publisherClient);

        // OpenTelemetry test dependencies
        services.AddSingleton<InMemoryActivityTracker>();

        services.AddEventPropagationPublisher();
        services.AddEventPropagationSubscriber()
            .AddDomainEventHandler<SambleDomainEventHandler>();

        services.AddSingleton(clientFactory);

        services.AddOptions<EventPropagationPublisherOptions>()
            .Configure(opt =>
            {
                opt.TopicName = "TopicName";
                opt.TopicEndpoint = "https://topic.endpoint";
                opt.TopicAccessKey = "TopicAccessKey";
            });

        return services;
    }
}