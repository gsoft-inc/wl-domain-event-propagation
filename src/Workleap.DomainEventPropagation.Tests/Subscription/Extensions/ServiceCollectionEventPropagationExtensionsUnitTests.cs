using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Workleap.DomainEventPropagation.Extensions;

namespace Workleap.DomainEventPropagation.Tests.Subscription.Extensions;

public class ServiceCollectionEventPropagationExtensionsUnitTests
{
    [Fact]
    public void AddEventPropagationSubscriber_WhenServicesIsNull_ThenThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddEventPropagationSubscriber());
    }

    [Fact]
    public void AddEventPropagationSubscriber_WhenServicesAreDefined_ThenReturnsEventPropagationSubscriberBuilderInstance()
    {
        var services = A.Fake<IServiceCollection>();

        var builder = services.AddEventPropagationSubscriber();

        Assert.NotNull(builder);
        Assert.IsType<EventPropagationSubscriberBuilder>(builder);
    }

    [Fact]
    public void AddEventPropagationSubscriber_WhenConfigureIsNull_ThenThrowsArgumentNullException()
    {
        var services = A.Fake<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(() => services.AddEventPropagationSubscriber(null!));
    }

    [Fact]
    public void AddEventPropagationSubscriber_WhenServicesAndConfigureAreDefined_ThenReturnsEventPropagationSubscriberBuilderInstance()
    {
        var services = A.Fake<IServiceCollection>();

        var builder = services.AddEventPropagationSubscriber(_ => { });

        Assert.NotNull(builder);
        Assert.IsType<EventPropagationSubscriberBuilder>(builder);
    }
}