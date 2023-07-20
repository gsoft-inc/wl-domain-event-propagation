using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Extensions;

public class ServiceCollectionEventPropagationExtensionsIntegrationTests
{
    private readonly IServiceCollection _services;

    private static readonly IReadOnlyList<string> AllTopics = new List<string>
    {
        "Organization",
        "Users"
    };

    public ServiceCollectionEventPropagationExtensionsIntegrationTests()
    {
        this._services = new ServiceCollection();
    }

    [Fact]
    public void AddEventPropagationSubscriber_WithoutConfigurationOverrides_ThenOptionsAreKeptIntact()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"{EventPropagationSubscriberOptions.SectionName}:SubscribedTopics:0", AllTopics[0] },
                { $"{EventPropagationSubscriberOptions.SectionName}:SubscribedTopics:1", AllTopics[1] },
            })
            .Build();

        this._services
            .AddSingleton<IConfiguration>(configuration)
            .AddEventPropagationSubscriber();

        using var serviceProvider = this._services.BuildServiceProvider();

        var subscriberOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationSubscriberOptions>>().Value;

        Assert.True(subscriberOptions.SubscribedTopics.SequenceEqual(AllTopics));
    }

    [Fact]
    public void AddEventPropagationSubscriber_WithConfigurationOverrides_ThenOptionsAreAsExpected()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                // We omit the last topic in the original configuration
                { $"{EventPropagationSubscriberOptions.SectionName}:SubscribedTopics:0", AllTopics[0] },
            })
            .Build();

        this._services
            .AddSingleton<IConfiguration>(configuration)
            .AddEventPropagationSubscriber(opts =>
            {
                // We add the last topic dynamically
                opts.SubscribedTopics.Add(AllTopics[1]);
            });

        using var serviceProvider = this._services.BuildServiceProvider();

        var subscriberOptions = serviceProvider.GetRequiredService<IOptions<EventPropagationSubscriberOptions>>().Value;

        Assert.True(subscriberOptions.SubscribedTopics.SequenceEqual(AllTopics));
    }
}