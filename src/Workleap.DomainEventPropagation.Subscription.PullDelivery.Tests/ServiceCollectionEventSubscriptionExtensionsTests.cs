using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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