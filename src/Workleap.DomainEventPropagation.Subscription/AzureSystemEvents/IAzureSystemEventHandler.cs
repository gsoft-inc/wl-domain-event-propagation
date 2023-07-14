namespace Workleap.DomainEventPropagation.AzureSystemEvents;

public interface IAzureSystemEventHandler<in TAzureSystemEvent>
{
    Task HandleAzureSystemEventAsync(TAzureSystemEvent azureSystemEvent, CancellationToken cancellationToken);
}