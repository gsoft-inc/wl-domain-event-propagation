using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Azure.Messaging.EventGrid;

using Fasterflect;

using Microsoft.ApplicationInsights.DataContracts;

using Newtonsoft.Json;

using OpenTelemetry.Trace;

using Workleap.EventPropagation.Abstractions;
using Workleap.EventPropagation.Telemetry;

namespace Workleap.DomainEventPropagation;

internal sealed class DomainEventGridWebhookHandler : IDomainEventGridWebhookHandler
{
    private const string DomainEventHandlerHandleMethod = "HandleDomainEventAsync";

    private static IEnumerable<Assembly> DomainEventAssemblies;

    private readonly IServiceProvider _serviceProvider;
    private readonly ISubscriptionTopicValidator _subscriptionTopicValidator;
    private readonly ITelemetryClientProvider _telemetryClientProvider;

    public DomainEventGridWebhookHandler(
        IServiceProvider serviceProvider,
        ISubscriptionTopicValidator subscriptionTopicValidator,
        ITelemetryClientProvider telemetryClientProvider)
    {
        this._serviceProvider = serviceProvider;
        this._subscriptionTopicValidator = subscriptionTopicValidator;
        this._telemetryClientProvider = telemetryClientProvider;

        DomainEventAssemblies = GetAssemblies();
    }

    public async Task HandleEventGridWebhookEventAsync(EventGridEvent eventGridEvent, CancellationToken cancellationToken)
    {
        if (!this._subscriptionTopicValidator.IsSubscribedToTopic(eventGridEvent.Topic))
        {
            this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventRejectedBasedOnTopic, $"Domain event received and ignored based on topic. Topic: ­{eventGridEvent.Topic}", eventGridEvent.EventType);

            return;
        }

        foreach (var assembly in DomainEventAssemblies)
        {
            var domainEventType = assembly.GetType(eventGridEvent.EventType);

            if (domainEventType != null)
            {
                try
                {
                    var deserializedEvent = JsonConvert.DeserializeObject(eventGridEvent.Data.ToString(), domainEventType);
                    var domainEvent = (IDomainEvent)deserializedEvent;

                    await this.HandleDomainEventAsync(domainEvent, domainEventType, cancellationToken);
                }
                catch (JsonSerializationException)
                {
                    // Mismatch in lib versions between publisher and subscriber can cause event data interpretation as double serialization
                    // fall back on double deserialization and see if it works before throwing exception
                    var deserializedEvent = JsonConvert.DeserializeObject(JsonConvert.DeserializeObject(eventGridEvent.Data.ToString()).ToString(), domainEventType);
                    var domainEvent = (IDomainEvent)deserializedEvent;

                    await this.HandleDomainEventAsync(domainEvent, domainEventType, cancellationToken);
                }

                return;
            }
        }

        this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventDeserializationFailed, $"Domain event received. Cannot deserialize object", eventGridEvent.EventType);
    }

    private async Task HandleDomainEventAsync(IDomainEvent domainEvent, Type domainEventTypeOf, CancellationToken cancellationToken)
    {
        var domainEventType = domainEventTypeOf ?? domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);

        var handler = this._serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventNoHandlerFound, $"No domain event handler found of type {handlerType.FullName}.", domainEventType.FullName);

            return;
        }

        using (this._telemetryClientProvider.StartOperation(new DependencyTelemetry("DomainEventHandler", domainEventType.Name, handler.GetType().Name, handler.GetType().FullName)))
        {
            await (Task)handler.CallMethod(DomainEventHandlerHandleMethod, domainEvent, cancellationToken);
        }


        this._telemetryClientProvider.TrackEvent(TelemetryConstants.DomainEventHandled, $"Domain event received and handled by domain event handler: {handlerType}", domainEventType.FullName);
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