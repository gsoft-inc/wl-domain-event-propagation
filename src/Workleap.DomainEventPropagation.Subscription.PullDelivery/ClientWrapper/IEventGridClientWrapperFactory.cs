namespace Workleap.DomainEventPropagation.ClientWrapper;

internal interface IEventGridClientWrapperFactory
{
    EventGridClientWrapper CreateClient(string name);
}