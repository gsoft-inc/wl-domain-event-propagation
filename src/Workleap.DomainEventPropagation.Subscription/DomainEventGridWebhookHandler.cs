using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure.Messaging.EventGrid;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private const string DomainEventHandlerHandleMethod = "HandleDomainEventAsync";

    private static readonly JsonSerializerOptions SerializerOptions = new();

    private static readonly IEnumerable<Assembly> DomainEventAssemblies = GetAssemblies();
    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriptionTopicValidator _subscriptionTopicValidator;
    private readonly ConcurrentDictionary<Type, MethodInfo> _handlerDictionary = new();

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        ISubscriptionTopicValidator subscriptionTopicValidator)
    {
        this._serviceProvider = serviceProvider;
        this._subscriptionTopicValidator = subscriptionTopicValidator;
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        if (!this._subscriptionTopicValidator.IsSubscribedToTopic(eventGridEvent.Topic))
        {
            return;
        }

        foreach (var assembly in DomainEventAssemblies)
        {
            var domainEventType = assembly.GetType(eventGridEvent.EventType);

            if (domainEventType is null)
            {
                continue;
            }

            var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(eventGridEvent.Data.ToString(), domainEventType, SerializerOptions);
            if (domainEvent == null)
            {
                continue;
            }

            await this.HandleDomainEventAsync(domainEvent, domainEventType, cancellationToken);

            return;
        }
    }

    private async Task HandleDomainEventAsync(IDomainEvent domainEvent, Type domainEventType, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);

        var handler = this._serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            return;
        }

        var handlerMethod = this._handlerDictionary.GetOrAdd(handlerType, static type =>
        {
            return type.GetMethod(DomainEventHandlerHandleMethod, BindingFlags.Public | BindingFlags.Instance) ??
                   throw new InvalidOperationException($"No public method found with name {DomainEventHandlerHandleMethod} on type {type.FullName}.");
        });

        await (Task)handlerMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
    }

    private static IEnumerable<Assembly> GetAssemblies()
    {
        // we target Workleap assemblies to limit processing time and exclude Workleap.EventPropagation.Common because it
        // has references that would make a .Net Framework (as opposed to netstandard or core) project to fail
        var domainEventType = typeof(IDomainEvent);
        var domainEventAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.FullName.StartsWith("Workleap") && !x.FullName.StartsWith("Workleap.EventPropagation.Common,"))
            .SelectMany(s => s.GetTypes())
            .Where(p => !p.IsInterface && !p.IsAbstract && domainEventType.IsAssignableFrom(p))
            .Select(x => x.Assembly)
            .Distinct()
            .ToArray();

        if (domainEventAssemblies.Any())
        {
            return domainEventAssemblies;
        }

        // return nothing, there are no domain events in entire project
        return Enumerable.Empty<Assembly>();
    }
}