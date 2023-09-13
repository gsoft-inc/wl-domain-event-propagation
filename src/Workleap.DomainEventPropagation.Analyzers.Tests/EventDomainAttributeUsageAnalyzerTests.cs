namespace Workleap.DomainEventPropagation.Analyzers.Tests;

public class EventDomainAttributeUsageAnalyzerTests : BaseAnalyzerTest<EventDomainAttributeUsageAnalyzer>
{
    private const string TestClassName = "MeowDomainEvent";

    [Fact]
    public async Task Given_NoAttribute_When_Analyze_Then_Diagnostics()
    {
        const string source = @"
public class MeowDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(EventDomainAttributeUsageAnalyzer.UseDomainEventAttribute, startLine: 2, startColumn: 14, endLine: 2, endColumn: 29, TestClassName)
            .RunAsync();
    }

    [Fact]
    public async Task Given_EventAttribute_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""Meow"")]
public class MeowDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }
}