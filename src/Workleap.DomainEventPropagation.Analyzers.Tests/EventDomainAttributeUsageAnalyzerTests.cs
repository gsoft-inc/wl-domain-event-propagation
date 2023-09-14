namespace Workleap.DomainEventPropagation.Analyzers.Tests;

public class EventDomainAttributeUsageAnalyzerTests : BaseAnalyzerTest<EventDomainAttributeUsageAnalyzer>
{
    private const string TestClassName = "SampleDomainEvent";

    [Fact]
    public async Task Given_NoAttribute_When_Analyze_Then_Diagnostics()
    {
        const string source = @"
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(EventDomainAttributeUsageAnalyzer.UseDomainEventAttribute, startLine: 2, startColumn: 14, endLine: 2, endColumn: 31, TestClassName)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""SampleDomainEvent"")]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Struct_With_DomainEventAttribute_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""SampleDomainEvent"")]
public record SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Random_Attribute_When_Analyze_Then_Diagnostics()
    {
        const string source = @"
[Serializable]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(EventDomainAttributeUsageAnalyzer.UseDomainEventAttribute, startLine: 3, startColumn: 14, endLine: 3, endColumn: 31, TestClassName)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_No_Interface_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""Sample"")]
public class SampleDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Multiple_Attributes_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""SampleDomainEvent"")]
[Serializable]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Wrong_Value_When_Analyze_Then_Diagnostic()
    {
        const string source = @"
[DomainEvent(""Sample"")]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .WithExpectedDiagnostic(EventDomainAttributeUsageAnalyzer.UseSameNameForAttributeValueAndClass, startLine: 3, startColumn: 14, endLine: 3, endColumn: 31, TestClassName)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Right_Value_When_Analyze_Then_No_Diagnostic()
    {
        const string source = @"
[DomainEvent(""SampleDomainEvent"")]
public class SampleDomainEvent : IDomainEvent
{    
}";

        await this.WithSourceCode(source)
            .RunAsync();
    }
}