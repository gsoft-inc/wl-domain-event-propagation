using System.Threading;

using Microsoft.ApplicationInsights.DataContracts;
using System.Threading.Tasks;

namespace Workleap.EventPropagation.Subscription;

public interface IEventGridRequestHandler
{
    Task<EventGridRequestResult> HandleRequestAsync(object requestContent, CancellationToken cancellationToken, RequestTelemetry requestTelemetry = default);
}