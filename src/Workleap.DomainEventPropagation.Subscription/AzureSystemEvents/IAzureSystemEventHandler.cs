namespace Workleap.DomainEventPropagation.AzureSystemEvents;

internal interface IAzureSystemEventHandler<in TAzureSystemEvent>
{
    Task HandleAzureSystemEventAsync(TAzureSystemEvent azureSystemEvent, CancellationToken cancellationToken);
}