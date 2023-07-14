using System.Threading;
using System.Threading.Tasks;

namespace Workleap.EventPropagation.Subscription.AzureSystemEvents;

public interface IAzureSystemEventHandler<in TAzureSystemEvent>
{
    Task HandleAzureSystemEventAsync(TAzureSystemEvent azureSystemEvent, CancellationToken cancellationToken);
}