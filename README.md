# Workleap.DomainEventPropagation

[![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.svg?logo=nuget)](https://www.nuget.org/packages/Workleap.DomainEventPropagation/)
[![build](https://img.shields.io/github/actions/workflow/status/gsoft-inc/workleap-domain-event-propagation/publish.yml?logo=github&branch=main)](https://github.com/gsoft-inc/workleap-domain-event-propagation/actions/workflows/publish.yml)

## Getting started

|Package| Download Link                                                                                        | Description                                                                |
|----|------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------|
|Workleap.DomainEventPropagation.Abstractions| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Abstractions.svg?logo=nuget) | Contains abstractions that are used for publishing and receiving events |
|Workleap.DomainEventPropagation.Publishing| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Publishing.svg?logo=nuget)   | Contains classes to publish events                                    |
|Workleap.DomainEventPropagation.Subscription| ![nuget](https://img.shields.io/nuget/v/Workleap.DomainEventPropagation.Subscription.svg?logo=nuget) |  Contains classes for subscriptions                                        |

### What does the Workleap.DomainEventPropagation package do?
* Provides list of existing Topics
* Serializes domain events
* Manages publication fields
* Provides webhook endpoint (.net core only)
* Provides IDomainEvent and IDomainEventHandler<> interfaces
* Deserializes domain events
* Mediates call to DomainEventHandlers
* Validates configurations
* Handles topic subscriptions

### Using the Workleap.DomainEventPropagation.Publishing package to publish events

When using dotnet core, you can register event propagation at startup in the service collection.
```
// Method 1: Lazily bind the options to a configuration section
services.AddEventPropagationPublisher();
services.AddOptions<EventPropagationPublisherOptions>().BindConfiguration(EventPropagationPublisherOptions.SectionName);

// Method 2: Set options values directly in C#
services.AddEventPropagationPublisher(opt =>
{
  opt.TopicName = "<topic_name_to _publish_to>",
  opt.TopicAccessKey = "<provided from keyVault>",
  opt.TopicEndpoint = "<azure_topic_uri>"
});
```
Configuration is required. Configuration can be loaded from the appsettings file by passing the IConfiguration instance (see above). The topic access key should be stored securely in a key vault.
```json
// appsettings.json
{
  "EventPropagation": {
    "TopicName": "<topic_name_to_publish_to>",
    "TopicAccessKey": "<keyVault_provided_value>",
    "TopicEndpoint": "<azure_topic_uri>"
  }
}
```
To publish an event, use the IEventPropagationClient interface (via injection). Use the PublishDomainEventAsync to publish the event. The required Subject field is a string description to provide context for the event.

```
var domainEvent = new ExampleDomainEvent
{
    Id = Guid.NewGuid().ToString(),
    Date = DateTime.UtcNow
};

await this._eventPropagationClient.PublishDomainEventAsync(subject: "TestEventPublication", coolCandorEvent);
```

#### Restrictions
* Your service can publish events on only 1 topic.
* The topic the service publishes to must be in the Topics class provided by the Workleap.EventPropagation package.

### Using the Workleap.EventPropagation package to subscribe to events
When using dotnet core, you can register event propagation subscriptions at startup in the service collection.  To configure the subscriber, the list of subscribed topics is required.

```
services
    .AddEventPropagationSubscriber()
    .AddDomainEventHandlersFromAssembly(Assembly.GetExecutingAssembly())
    .Configure(new[] { Topics.Organisation, Topics.Candor });

// or add a single domain event handler
.AddDomainEventHandler<ExampleDomainEventHandler>()
```
An API endpoint must be made available to receive events from the EventGrid topic. The package provides this endpoint for dotnet core projects. It can be added like so.
```
services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(EventGridController).Assembly));
```

#### Define domain event handlers

You must define domain event handlers for events you wish to handle. You are not obligated to handle every possible event from a topic.

Domain event handlers must implement the IDomainEventHandler<> interface.

```
public class ExampleDomainEventHandler : IDomainEventHandler<ExampleDomainEvent>
{
    public Task HandleDomainEventAsync(ExampleDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
```
#### Restrictions
* You may only define 1 domain event handler per domain event you wish to handle. If you would require more, use the 1 allowed domain event handler as a façade for multiple operations.
* DomainEventHandlers must have idempotent behavior (you could execute it multiple times for the same event and the result would always be the same).
* DomainEventHandlers should use the property DataVersion (when necessary)

### Exposing domain events

To expose your service’s domain events, you must provide a NuGet package with all domain (class) definitions. Your project must reference the Workleap.EventPropagation package and your domain events must implement the IDomainEvent interface.

```
public class ExampleDomainEvent : IDomainEvent
{
    public string CoolId { get; set; }

    public DateTime CoolDate { get; set; }

    public string DataVersion => "1";
}
```

It is recommended to use a seperate pipeline to publish domain event definitions. Any existing pipeline can be reused with minor tweaks to achieve this.

#### Restrictions
* Domain events must only contain Ids and types (any content is optional). In some rare cases, an immutable field could be added to the event, however this comes at risk of exposing PII information.
* Changes to domain events should be additive
* Domain events must implement the DataVersion property
* Domain event projects should follow the pattern Workleap.[Service].[DomainEvents] (Service would usually match the topic name).
* EventGrid limits event size to 64 kb. This means a regular event could contain a maximum of about 1500 Guid ids. Keep in mind the size restriction when sending batch events. Do not hesitate to split a larger event into multiple, smaller domain events.

## Building, releasing and versioning

The project can be built by running `Build.ps1`. It uses [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) to help detect public API breaking changes. Use the built-in roslyn analyzer to ensure that public APIs are declared in `PublicAPI.Shipped.txt`, and obsolete public APIs in `PublicAPI.Unshipped.txt`.

A new *preview* NuGet package is **automatically published** on any new commit on the main branch. This means that by completing a pull request, you automatically get a new NuGet package.

When you are ready to **officially release** a stable NuGet package by following the [SemVer guidelines](https://semver.org/), simply **manually create a tag** with the format `x.y.z`. This will automatically create and publish a NuGet package for this version.


## License

Copyright © 2023, Workleap This code is licensed under the Apache License, Version 2.0. You may obtain a copy of this license at https://github.com/gsoft-inc/gsoft-license/blob/master/LICENSE.
