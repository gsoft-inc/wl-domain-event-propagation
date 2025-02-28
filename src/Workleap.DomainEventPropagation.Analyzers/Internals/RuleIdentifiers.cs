namespace Workleap.DomainEventPropagation.Analyzers.Internals;

internal static class RuleIdentifiers
{
    public const string HelpUri = "https://github.com/workleap/wl-domain-event-propagation";

    // DO NOT change the identifier of existing rules.
    // Projects can customize the severity level of analysis rules using a .editorconfig file.
    public const string UseDomainEventAttribute = "WLDEP01";

    public const string UseUniqueNameForAttributeValue = "WLDEP02";

    public const string FollowNamingConventionAttributeValue = "WLDEP03";
}