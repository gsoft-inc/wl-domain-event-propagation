# Workleap.DomainEventPropagation

[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/workleap-domain-event-propagation/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/workleap-domain-event-propagation/actions/workflows/publish.yml)

|Package| Download Link                                                                                        | Description                                                                |
|----|------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------|
|Workleap.DomainEventPropagation.Abstractions| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Abstractions.svg?logo=nuget) | Contains abstractions that are used for publishing and receiving events |
|Workleap.DomainEventPropagation.Publishing| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Publishing.svg?logo=nuget)   | Contains classes to publish events                                    |
|Workleap.DomainEventPropagation.Subscription| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Subscription.svg?logo=nuget) |  Contains classes to subscribe to topics and receive events                                        |

This set of three libraries is meant to be used in conjunction with [Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/) in order to publish and receive domain events. It is meant to be used in a multi-services architecture where each service is responsible for its own data and publishes events to notify other services of changes.

## Getting started

### Publisher library
Install the package [Workleap.DomainEventPropagation.Publishing](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Publishing) in your webapi project that wants to send events to Event Grid. Then you can use on of the following methods to register the required services.

```csharp
// Method 1: Lazily bind the options to a configuration section
services.AddEventPropagationPublisher();
services.AddOptions<EventPropagationPublisherOptions>()
    .BindConfiguration(EventPropagationPublisherOptions.SectionName);

// appsetting.json
{
  "EventPropagation": {
    "TopicName": "<topic_name_to_publish_to>",
    "TopicAccessKey": "<keyVault_provided_value>",
    "TopicEndpoint": "<azure_topic_uri>"
  }
}

// Method 2: Lazily bind the options to a configuration section and use a TokenCredential and RBAC
services.AddEventPropagationPublisher(opt =>
{
    opt.TokenCredential = new DefaultAzureCredential();
});
services.AddOptions<EventPropagationPublisherOptions>()
    .BindConfiguration(EventPropagationPublisherOptions.SectionName);

// appsetting.json
{
  "EventPropagation": {
    "TopicName": "<topic_name_to_publish_to>",
    "TopicEndpoint": "<azure_topic_uri>"
  }
}

// Method 3: Set options values directly in C# using and access key 
services.AddEventPropagationPublisher(opt =>
{   
    opt.TopicName = "<topic_name_to_publish_to>",
    opt.TopicEndpoint = "<azure_topic_uri>",
    opt.TopicAccessKey = "<provided from keyVault>"
    
});

// Method 4: Set options values directly in C# using a TokenCredential and RBAC
services.AddEventPropagationPublisher(opt => 
{
    opt.TopicName = "<topic_name_to_publish_to>",
    opt.TopicEndpoint = "<azure_topic_uri>",
    o.TokenCredential = new DefaultAzureCredential())
}
```
*Note that you can use either an access key or a token credentials in order to authenticate with your eventGrid topic but not both.*

Now in order to publish a domain event, you first need to define your domain events using the `IDomainEvent` interface.
```csharp
public class ExampleDomainEvent : IDomainEvent
{
    public string Id { get; set; }

    public DateTime Date { get; set; }
}
```

Once your domain events are defined, you can use the `IEventPropagationClient` interface (via dependency injection).
```csharp
var domainEvent = new ExampleDomainEvent
{
    Id = Guid.NewGuid().ToString(),
    Date = DateTime.UtcNow
};

await this._eventPropagationClient.PublishDomainEventAsync(subject: "TestEventPublication", domainEvent);
```

### Subscriber library

Install the package [Workleap.DomainEventPropagation.Subscription](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Subscription) in your webapi project that wants to receive events from Event Grid. Then you can use on of the following methods to register the required services.

```csharp
// Method 1: Register only selected domain event handlers
services.AddEventPropagationSubscriber()
    .AddDomainEventHandler<ExampleDomainEventHandler>()
    .AddDomainEventHandler<SampleDomainEventHandler>();

// Method 2: Register all domain event handlers from an assembly
services
    .AddEventPropagationSubscriber()
    .AddDomainEventHandlersFromAssembly(Assembly.GetExecutingAssembly());


// Register the webhook endpoint
app.UseEndpoints(endpointBuilder =>
{
    endpointBuilder
        .AddEventPropagationEndpoint()
        .WithAuthorization();
});
```

You can define your domain event handlers by implementing the `IDomainEventHandler<>` interface and then registering them in the service collection as shown above.

```csharp
public class ExampleDomainEventHandler : IDomainEventHandler<ExampleDomainEvent>
{
    public Task HandleDomainEventAsync(ExampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```

#### Securing your webhook endpoint
It is required to expose an endpoint in order for eventGrid to be able to push events. By default, the registered endpoint will allow anonymous access, but it is possible to secure it as shown below:

```csharp
app.UseEndpoints(endpointBuilder =>
{
    endpointBuilder
        .AddEventPropagationEndpoint()
        .WithAuthorization(); // This here will add an authorize attribute to the endpoint requiring a valid JTW access token.
});
```

Now that your api endpoint is secure with an authorize attribute, you just need to follow the remaining steps:
- Create a `AzureEventGridSecureWebhookSubscriber` role on your webhook application's app registration.
- Grant said role to Microsoft.EventGrid service principal
- Create a webhook EventGrid subscription and specify the TenantId and webhook applicationId

For more details about each of those steps, you can follow this Microsoft [documentation](https://learn.microsoft.com/en-us/azure/event-grid/secure-webhook-delivery#deliver-events-to-a-webhook-in-the-same-azure-ad-tenant).

### Additional Notes
* You may only define one domain event handler per domain event you wish to handle. If you would require more, use the single allowed domain event handler as a facade for multiple operations.
* DomainEventHandlers must have idempotent behavior (you could execute it multiple times for the same event and the result would always be the same).

## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.


## License

Copyright © 2023, Workleap This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
