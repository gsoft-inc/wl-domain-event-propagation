using Polly;

namespace Workleap.DomainEventPropagation.Analyzers.Tests;

public class EventDomainAttributeUsageAnalyzerTests : BaseAnalyzerTest<EventDomainAttributeUsageAnalyzer>
{
    [Fact]
    public async Task Given_NoAttribute_When_Analyze_Then_Diagnostics()
    {
        const string source = """
public class {|WLDEP01:SampleDomainEvent|} : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_When_Analyze_Then_No_Diagnostic()
    {
        const string source = """
[DomainEvent("SampleDomainEvent")]
public class SampleDomainEvent : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Struct_With_DomainEventAttribute_When_Analyze_Then_No_Diagnostic()
    {
        const string source = """
[DomainEvent("SampleDomainEvent")]
public record SampleDomainEvent : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_Random_Attribute_When_Analyze_Then_Diagnostics()
    {
        const string source = """
[Serializable]
public class {|WLDEP01:SampleDomainEvent|} : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_No_Interface_When_Analyze_Then_No_Diagnostic()
    {
        const string source = """
[DomainEvent("Sample")]
public class SampleDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Multiple_Attributes_When_Analyze_Then_No_Diagnostic()
    {
        const string source = """
[DomainEvent("SampleDomainEvent")]
[Serializable]
public class SampleDomainEvent : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Non_Unique_Value_When_Analyze_Then_Diagnostic()
    {
        const string source = """
[DomainEvent("SampleDomainEvent")]
public class SampleDomainEvent : IDomainEvent
{    
}
[DomainEvent("SampleDomainEvent")]
public class {|WLDEP02:SampleDomainEvent2|} : IDomainEvent
{    
}
"""; 

        // Retrying this test to ensure expected diagnostic by line numbers.
        // We don't know exactly which of the above defined "source" will get
        // executed by analyzer first because of the concurrent execution.
        var retryPolicy = Policy.Handle<Exception>().RetryAsync(10);
        await retryPolicy.ExecuteAsync(async () =>
        {
            await new BaseAnalyzerTest<EventDomainAttributeUsageAnalyzer>()
                .WithSourceCode(source)
                .RunAsync();
        });
    }

    [Fact]
    public async Task Given_DomainEventAttribute_With_Unique_Value_When_Analyze_Then_No_Diagnostic()
    {
        const string source = """
[DomainEvent("SampleDomainEvent")]
public class SampleDomainEvent : IDomainEvent
{    
}
[DomainEvent("SampleDomainEvent2")]
public class SampleDomainEvent2 : IDomainEvent
{    
}
""";

        await this.WithSourceCode(source)
            .RunAsync();
    }
}