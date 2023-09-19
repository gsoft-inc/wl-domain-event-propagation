# Workleap.DomainEventPropagation

[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/workleap-domain-event-propagation/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/workleap-domain-event-propagation/actions/workflows/publish.yml)

| Package                                                          | Download Link                                                                                                                                                                                                               | Description                                                                                 |
|------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| Workleap.DomainEventPropagation.Abstractions                     | [![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Abstractions.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Abstractions)                                         | Contains abstractions that are used for publishing and receiving events                     |
| Workleap.DomainEventPropagation.Publishing                       | [![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Publishing.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Publishing)                                             | Contains types used to publish events from any kind of .NET application                     |
| Workleap.DomainEventPropagation.Publishing.ApplicationInsights   | [![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Publishing.ApplicationInsights.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Publishing.ApplicationInsights)     | Adds Application Insights distributed tracing when publishing events                        |
| Workleap.DomainEventPropagation.Subscription                     | [![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Subscription.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Subscription)                                         | Contains types for an ASP.NET Core app to subscribe to Event Grid topics and receive events |
| Workleap.DomainEventPropagation.Subscription.ApplicationInsights | [![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Subscription.ApplicationInsights.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Subscription.ApplicationInsights) | Adds Application Insights distributed tracing when receiving events                         |

These libraries must be used in conjunction with [Azure Event Grid](https://learn.microsoft.com/en-us/azure/event-grid/) in order to publish and receive domain events.
It is meant to be used in a multi-services architecture where each service is responsible for its own data and publishes events to notify other services of changes.


## Getting started

### Publish domain events

Install the package [Workleap.DomainEventPropagation.Publishing](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Publishing) in your .NET project that wants to send events to an Event Grid topic.
Then, you can use one of the following methods to register the required services.

```csharp
// Method 1: Automatically bind the options to a well-known configuration section
services.AddEventPropagationPublisher();

// appsetting.json (or any other configuration source)
{
  "EventPropagation": {
    "Publisher": {
      "TopicEndpoint": "<azure_topic_uri>",
      "TopicAccessKey": "<secret_value>"
    }
  }
}

// Method 2: Automatically bind the options to a well-known configuration section with Azure Identity (RBAC)
services.AddEventPropagationPublisher(options =>
{
    options.TokenCredential = new DefaultAzureCredential();
});

// appsetting.json (or any other configuration source)
{
  "EventPropagation": {
    "Publisher": {
      "TopicEndpoint": "<azure_topic_uri>"
    }
  }
}

// Method 3: Set options values directly in C#
services.AddEventPropagationPublisher(options =>
{
    options.TopicEndpoint = "<azure_topic_uri>",

    // Using an access key        
    options.TopicAccessKey = "<secret_value>"
    
    // Using Azure Identity (RBAC)
    options.TokenCredential = new DefaultAzureCredential();
});
```

> Note that you can use either an access key or a token credential in order to authenticate to your eventGrid topic, not both.

Then, in order to publish a domain event, you first need to define your domain events using the `IDomainEvent` interface.
Decorate the domain event with the `[DomainEvent]` attribute, specifying a unique event name.

```csharp
[DomainEvent("example")]
public class ExampleDomainEvent : IDomainEvent
{
    public string Id { get; set; }
}
```

Once your domain events are defined, you can inject and use the `IEventPropagationClient` service.

```csharp
var domainEvent = new ExampleDomainEvent
{
    Id = Guid.NewGuid().ToString()
};

await this._eventPropagationClient.PublishDomainEventAsync(domainEvent, CancellationToken.None);
```


### Subscribe to domain events

Install the package [Workleap.DomainEventPropagation.Subscription](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Subscription) in your ASP.NET Core project that wants to receive events from Event Grid topics.

You can define your domain event handlers by implementing the `IDomainEventHandler<>` interface and then registering them in the service collection later.

```csharp
public class ExampleDomainEventHandler : IDomainEventHandler<ExampleDomainEvent>
{
    public Task HandleDomainEventAsync(ExampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Do something with the domain event
        return Task.CompletedTask;
    }
}
```

Then, you can use on of the following methods to register the required services and map the webhook endpoint.

```csharp
// Method 1: Register only selected domain event handlers
services.AddEventPropagationSubscriber()
    .AddDomainEventHandler<ExampleDomainEventHandler>()
    .AddDomainEventHandler<OtherDomainEventHandler>();

// Method 2: Register all domain event handlers from a given assembly
services.AddEventPropagationSubscriber()
    .AddDomainEventHandlersFromAssembly(Assembly.GetExecutingAssembly());

// Register the webhook endpoint in your ASP.NET Core app (startup-based approach)
app.UseEndpoints(builder =>
{
    builder.MapEventPropagationEndpoint();
});

// Register the webhook endpoint in your ASP.NET Core app (minimal APIs approach)
app.MapEventPropagationEndpoint();
```


#### Securing the webhook endpoint

It is required to expose an ASP.NET Core endpoint in order for Event Grid topics to be able to push events.
By default, the registered endpoint will allow anonymous access, but it is possible to secure it as shown below:

```csharp
// "RequireAuthorization" is a built-in ASP.NET Core method so you can specify any authorization policy you want
app.MapEventPropagationEndpoint().RequireAuthorization();
```

Now, follow this [Microsoft documentation](https://learn.microsoft.com/en-us/azure/event-grid/secure-webhook-delivery#deliver-events-to-a-webhook-in-the-same-azure-ad-tenant) to continue the configuration.  


## Additional notes

* You may only define one domain event handler per domain event you wish to handle. If you would require more, use the single allowed domain event handler as a facade for multiple operations.
* Domain event handlers must have idempotent behavior (you could execute it multiple times for the same event and the result would always be the same).
* If your domain event types and handlers are in dedicated assemblies, you can reference the [Workleap.DomainEventPropagation.Abstractions](https://www.nuget.org/packages/Workleap.DomainEventPropagation.Abstractions) packages in order to avoid a dependency on third-parties like Azure and Microsoft extensions.

## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.

## Included Roslyn analyzers

| Rule ID | Category | Severity | Description                                                        |
|---------|----------|----------|--------------------------------------------------------------------|
| WLDEP01 | Usage    | Warning  | Use DomainEvent attribute on event                                 |
| WLDEP02 | Usage    | Warning  | Use unique event name in attribute                                 | 

To modify the severity of one of these diagnostic rules, you can use a `.editorconfig` file. For example:

```ini
## Disable analyzer for test files
[**Tests*/**.cs]
dotnet_diagnostic.WLDEP01.severity = none
dotnet_diagnostic.WLDEP02.severity = none
```

To learn more about configuring or suppressing code analysis warnings, refer to [this documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings).

## License

Copyright © 2023, Workleap This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
