using System.Threading.Tasks.Dataflow;

namespace Workleap.DomainEventPropagation;

internal sealed class CloudEventSerializationException : Exception
{
    public CloudEventSerializationException(string subject, Exception innerException)
        : base($"The cloud event with subject {subject} could not be wrapped into a DomainEventWrapper", innerException)
    {
    }
}
