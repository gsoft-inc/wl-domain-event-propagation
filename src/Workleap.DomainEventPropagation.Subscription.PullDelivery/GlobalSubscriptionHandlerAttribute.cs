namespace Workleap.DomainEventPropagation;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GlobalSubscriptionHandlerAttribute : Attribute
{
}