using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;

namespace Workleap.DomainEventPropagation.ClientWrapper;

internal class EventGridClientWrapperFactory : IEventGridClientWrapperFactory
{
    private readonly IAzureClientFactory<EventGridClient> _factory;

    public EventGridClientWrapperFactory(IAzureClientFactory<EventGridClient> factory)
    {
        this._factory = factory;
    }

    public EventGridClientWrapper CreateClient(string name)
    {
        var client = this._factory.CreateClient(name);
        return new EventGridClientWrapper(client);
    }
}