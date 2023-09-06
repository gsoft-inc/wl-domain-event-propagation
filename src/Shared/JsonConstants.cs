using System.Text.Json;

namespace Workleap.DomainEventPropagation;

internal static class JsonConstants
{
    public static readonly JsonSerializerOptions DomainEventWrapperSerializerOptions = new(JsonSerializerDefaults.Web);
}