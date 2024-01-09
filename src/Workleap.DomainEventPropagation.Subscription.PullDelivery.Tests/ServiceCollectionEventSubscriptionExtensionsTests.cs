using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.EventGrid.Namespaces;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests;

public class ServiceCollectionEventSubscriptionExtensionsTests
{
    private const string AccessKey = "accessKey";
    private const string TopicEndPoint = "http://topicurl.com";
    private const string TopicName = "topic-name";
    private const string SubscriptionName = "sub-name";

    [Fact]
    public void GivenUnnamedConfiguration_WhenAddSubscriber_ThenOptionsAreRegistered()
    {
        // Given
        var services = new ServiceCollection();
        GivenConfigurations(services, EventPropagationSubscriptionOptions.DefaultSectionName);

        // When
        services.AddPullDeliverySubscription().AddSubscriber();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>().Get(EventPropagationSubscriptionOptions.DefaultSectionName);

        // Then
        Assert.Equal(AccessKey, options.TopicAccessKey);
        Assert.Equal(TopicEndPoint, options.TopicEndpoint);
        Assert.Equal(TopicName, options.TopicName);
        Assert.Equal(SubscriptionName, options.SubscriptionName);
    }

    [Fact]
    public void GivenUnnamedConfiguration_WhenAddSubscriber_CanOverrideConfiguration()
    {
        // Given
        var services = new ServiceCollection();
        GivenConfigurations(services, EventPropagationSubscriptionOptions.DefaultSectionName);

        // When
        services.AddPullDeliverySubscription().AddSubscriber(options => { options.TopicEndpoint = "http://ovewrite.io"; });
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>().Get(EventPropagationSubscriptionOptions.DefaultSectionName);

        // Then
        Assert.Equal("http://ovewrite.io", options.TopicEndpoint);
        Assert.Equal(AccessKey, options.TopicAccessKey);
        Assert.Equal(SubscriptionName, options.SubscriptionName);
        Assert.Equal(TopicName, options.TopicName);
    }

    [Fact]
    public void GivenNamedConfigurations_WhenAddSubscribers_ThenOptionsAreRegistered()
    {
        // Given
        var services = new ServiceCollection();
        const string sectionName1 = "EventPropagation:Sub1";
        const string sectionName2 = "EventPropagation:Sub2";
        GivenConfigurations(services, sectionName1, sectionName2);

        // When
        services.AddPullDeliverySubscription()
            .AddSubscriber(sectionName1)
            .AddSubscriber(sectionName2);
        var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
        var options1 = monitor.Get(sectionName1);
        var options2 = monitor.Get(sectionName2);

        // Then
        Assert.Equal(AccessKey, options1.TopicAccessKey);
        Assert.Equal(TopicEndPoint, options1.TopicEndpoint);
        Assert.Equal(TopicName, options1.TopicName);
        Assert.Equal(SubscriptionName, options1.SubscriptionName);

        Assert.Equal(AccessKey, options2.TopicAccessKey);
        Assert.Equal(TopicEndPoint, options2.TopicEndpoint);
        Assert.Equal(TopicName, options2.TopicName);
        Assert.Equal(SubscriptionName, options2.SubscriptionName);
    }

    [Fact]
    public void GivenNamedConfigurations_WhenAddSubscribers_CanOverrideConfigurations()
    {
        // Given
        var services = new ServiceCollection();
        const string sectionName1 = "EventPropagation:Sub1";
        const string sectionName2 = "EventPropagation:Sub2";
        GivenConfigurations(services, sectionName1, sectionName2);

        // When
        services.AddPullDeliverySubscription()
            .AddSubscriber(options => { options.TopicEndpoint = "http://ovewrite1.io"; }, sectionName1)
            .AddSubscriber(options => { options.TopicEndpoint = "http://ovewrite2.io"; }, sectionName2);
        var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>();
        var options1 = monitor.Get(sectionName1);
        var options2 = monitor.Get(sectionName2);

        // Then
        Assert.Equal("http://ovewrite1.io", options1.TopicEndpoint);
        Assert.Equal(AccessKey, options1.TopicAccessKey);
        Assert.Equal(TopicName, options1.TopicName);
        Assert.Equal(SubscriptionName, options1.SubscriptionName);

        Assert.Equal("http://ovewrite2.io", options2.TopicEndpoint);
        Assert.Equal(AccessKey, options2.TopicAccessKey);
        Assert.Equal(TopicName, options2.TopicName);
        Assert.Equal(SubscriptionName, options2.SubscriptionName);
    }

    [Fact]
    public void GivenNamedConfigurations_WhenResolveClientDescriptors_ThenListEveryRegisteredConfiguration()
    {
        // Given
        var services = new ServiceCollection();
        const string sectionName1 = "EventPropagation:Sub1";
        const string sectionName2 = "EventPropagation:Sub2";
        GivenConfigurations(services, sectionName1, sectionName2);

        // When
        services.AddPullDeliverySubscription()
            .AddSubscriber(sectionName1)
            .AddSubscriber(sectionName2);
        var serviceProvider = services.BuildServiceProvider();
        var clientDescriptors = serviceProvider.GetRequiredService<IEnumerable<EventGridClientDescriptor>>().ToArray();

        // Then
        clientDescriptors.Should().HaveCount(2);
        clientDescriptors.Select(d => d.Name).Should().BeEquivalentTo(sectionName1, sectionName2);
    }

    [Fact]
    public void GivenNamedConfigurations_WhenResolveEventPuller_ThenCreatesEveryClients()
    {
        // Given
        var services = new ServiceCollection();
        const string sectionName1 = "EventPropagation:Sub1";
        const string sectionName2 = "EventPropagation:Sub2";
        GivenConfigurations(services, sectionName1, sectionName2);
        var fakeClientFactory = A.Fake<IAzureClientFactory<EventGridClient>>();
        services.Replace(new ServiceDescriptor(typeof(IAzureClientFactory<EventGridClient>), fakeClientFactory));
        services.AddTransient<ILogger<EventPuller>, NullLogger<EventPuller>>();
        services.AddTransient<ILogger<ICloudEventHandler>, NullLogger<CloudEventHandler>>();

        // When
        services.AddPullDeliverySubscription()
            .AddSubscriber(sectionName1)
            .AddSubscriber(sectionName2);
        var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IEnumerable<IHostedService>>();

        // Then
        A.CallTo(() => fakeClientFactory.CreateClient(sectionName1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeClientFactory.CreateClient(sectionName2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenNoSubscriber_WhenRegisterHandler_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var act = () => services.AddPullDeliverySubscription()
            .AddDomainEventHandler<SampleEvent, TestHandler>();

        // Then
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenNoSubscriber_WhenRegisterHandlerFromAssembly_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var act = () => services.AddPullDeliverySubscription()
            .AddDomainEventHandlers(Assembly.GetAssembly(typeof(ServiceCollectionEventSubscriptionExtensionsTests))!);

        // Then
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenMultipleHandlersForSameEvent_WhenRegisters_ThenOnlyFirstOneIsRegistered()
    {
        // Given
        var services = new ServiceCollection();

        // When
        services.AddPullDeliverySubscription()
            .AddSubscriber()
            .AddDomainEventHandler<SampleEvent, TestHandler>()
            .AddDomainEventHandler<SampleEvent, AnotherTestHandler>();

        // Then
        services.Count(x => x.ServiceType == typeof(IDomainEventHandler<SampleEvent>)).Should().Be(1);
        services.BuildServiceProvider().GetRequiredService<IDomainEventHandler<SampleEvent>>().Should().BeOfType<TestHandler>();
    }

    private static void GivenConfigurations(IServiceCollection services, params string[] sections)
    {
        var dictionnary = new Dictionary<string, string>();

        foreach (var section in sections)
        {
            dictionnary[$"{section}:{nameof(EventPropagationSubscriptionOptions.TopicAccessKey)}"] = AccessKey;
            dictionnary[$"{section}:{nameof(EventPropagationSubscriptionOptions.TopicEndpoint)}"] = TopicEndPoint;
            dictionnary[$"{section}:{nameof(EventPropagationSubscriptionOptions.TopicName)}"] = TopicName;
            dictionnary[$"{section}:{nameof(EventPropagationSubscriptionOptions.SubscriptionName)}"] = SubscriptionName;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dictionnary)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
    }

    [DomainEvent("an-event", EventSchema.CloudEvent)]
    public class SampleEvent : IDomainEvent
    {
        public string? Message { get; set; }
    }

    // This needs to be public for AddDomainEventHandlers to be able to find it
    public class TestHandler : IDomainEventHandler<SampleEvent>
    {
        public static ConcurrentQueue<SampleEvent> ReceivedEvents { get; } = new();

        public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            ReceivedEvents.Enqueue(domainEvent);
            return Task.CompletedTask;
        }
    }

    private class AnotherTestHandler : IDomainEventHandler<SampleEvent>
    {
        public static ConcurrentQueue<SampleEvent> ReceivedEvents { get; } = new();

        public Task HandleDomainEventAsync(SampleEvent domainEvent, CancellationToken cancellationToken)
        {
            ReceivedEvents.Enqueue(domainEvent);
            return Task.CompletedTask;
        }
    }
}