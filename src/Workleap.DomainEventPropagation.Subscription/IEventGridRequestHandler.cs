namespace Workleap.DomainEventPropagation;

public interface IEventGridRequestHandler
{
    Task<EventGridRequestResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken);
}