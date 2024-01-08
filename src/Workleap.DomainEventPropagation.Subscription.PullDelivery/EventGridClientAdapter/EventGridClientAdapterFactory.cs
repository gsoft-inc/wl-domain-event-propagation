using Azure.Messaging.EventGrid.Namespaces;
using Microsoft.Extensions.Azure;

namespace Workleap.DomainEventPropagation.EventGridClientAdapter;

internal sealed class EventGridClientAdapterFactory : IEventGridClientWrapperFactory
{
    private readonly IAzureClientFactory<EventGridClient> _factory;

    public EventGridClientAdapterFactory(IAzureClientFactory<EventGridClient> factory)
    {
        this._factory = factory;
    }

    public IEventGridClientAdapter CreateClient(string name)
    {
        var client = this._factory.CreateClient(name);
        return new EventGridClientAdapter(client);
    }
}