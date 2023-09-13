namespace Workleap.DomainEventPropagation.Analyzers.Tests;

public class EventDomainAttributeUsageAnalyzerTests : BaseAnalyzerTest<EventDomainAttributeUsageAnalyzer>
{
    private const string TestClassName = "SampleDomainEvent";

    [Fact]
    public async Task Given_RandomAttribute_When_Analyze_Then_Diagnostics()
    {
        const string source = @"
public class SampleDomainEvent : IDomainEvent
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
[DomainEvent(""Sample"")]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }
}