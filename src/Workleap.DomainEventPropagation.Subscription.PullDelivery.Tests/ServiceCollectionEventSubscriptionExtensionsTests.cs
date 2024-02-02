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
using Workleap.DomainEventPropagation.Subscription.PullDelivery.Tests.Events;

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
        services.AddPullDeliverySubscription().AddTopicSubscription();
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EventPropagationSubscriptionOptions>>().Get(EventPropagationSubscriptionOptions.DefaultSectionName);

        // Then
        Assert.Equal(AccessKey, options.TopicAccessKey);
        Assert.Equal(TopicEndPoint, options.TopicEndpoint);
        Assert.Equal(TopicName, options.TopicName);
        Assert.Equal(SubscriptionName, options.SubscriptionName);
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
            .AddTopicSubscription(sectionName1)
            .AddTopicSubscription(sectionName2);
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
            .AddTopicSubscription(sectionName1, options => { options.TopicEndpoint = "http://ovewrite1.io"; })
            .AddTopicSubscription(sectionName2, options => { options.TopicEndpoint = "http://ovewrite2.io"; });
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
            .AddTopicSubscription(sectionName1)
            .AddTopicSubscription(sectionName2);
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
        services.AddTransient<ILogger<ICloudEventHandler>, NullLogger<ICloudEventHandler>>();

        // When
        services.AddPullDeliverySubscription()
            .AddTopicSubscription(sectionName1)
            .AddTopicSubscription(sectionName2);
        var serviceProvider = services.BuildServiceProvider();
        _ = serviceProvider.GetRequiredService<IEnumerable<IHostedService>>();

        // Then
        A.CallTo(() => fakeClientFactory.CreateClient(sectionName1)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fakeClientFactory.CreateClient(sectionName2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void GivenNoSubscriber_WhenRegisterHandlerIndividually_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var act = () => services.AddPullDeliverySubscription()
            .AddDomainEventHandler<SampleEvent, SampleEventTestHandler>();

        // Then
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenNoSubscriber_WhenRegisterHandlersFromAssembly_ThenExceptionIsThrown()
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
    public void GivenMultipleHandlersForSameEvent_WhenRegistersThemIndividually_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var fct = () => services.AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandler<MisconfiguredTestAssembly.SampleEvent, MisconfiguredTestAssembly.SampleEventTestHandler>()
            .AddDomainEventHandler<MisconfiguredTestAssembly.SampleEvent, MisconfiguredTestAssembly.AnotherSampleEventTestHandler>();

        // Then
        fct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenMultipleHandlersForSameEvent_WhenIndividuallyRegisteredAndFromAssembly_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var fct = () => services.AddPullDeliverySubscription()
            .AddTopicSubscription()
            .AddDomainEventHandler<SampleEvent, SampleEventTestHandler>()
            .AddDomainEventHandlers(Assembly.GetAssembly(typeof(ServiceCollectionEventSubscriptionExtensionsTests))!);

        // Then
        fct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenMultipleHandlersForSameEventInAssembly_WhenRegisterHandlers_ThenExceptionIsThrown()
    {
        // Given
        var services = new ServiceCollection();

        // When
        var act = () => services.AddPullDeliverySubscription()
            .AddDomainEventHandlers(Assembly.GetAssembly(typeof(MisconfiguredTestAssembly.SampleEvent))!);

        // Then
        act.Should().Throw<InvalidOperationException>();
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
}