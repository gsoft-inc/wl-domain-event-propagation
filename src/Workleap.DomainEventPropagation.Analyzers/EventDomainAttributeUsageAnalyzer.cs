using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Workleap.DomainEventPropagation.Analyzers.Internals;
using Workleap.Extensions.MediatR.Analyzers.Internals;

namespace Workleap.DomainEventPropagation.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventDomainAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseDomainEventAttribute = new(
        id: RuleIdentifiers.UseDomainEventAttribute,
        title: "Use DomainEvent attribute on event",
        messageFormat: "Use the DomainEvent attribute",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseDomainEventAttribute);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStarted);
    }

    private static void OnCompilationStarted(CompilationStartAnalysisContext context)
    {
        var analyzer = new AnalyzerImplementation(context.Compilation);
        if (analyzer.IsValid)
        {
            context.RegisterSymbolAction(analyzer.AnalyzeOperationInvocation, SymbolKind.NamedType);
        }
    }

    private sealed class AnalyzerImplementation
    {
        private readonly INamedTypeSymbol? _domainEventType;
        private readonly INamedTypeSymbol? _domainEventAttributeType;
        private readonly ImmutableHashSet<INamedTypeSymbol> _domainEventTypes;

        public AnalyzerImplementation(Compilation compilation)
        {
            this._domainEventType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventInterface, KnownSymbolNames.WorkleapDomainEventPropagationAbstractionsAssembly)!;
            this._domainEventAttributeType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventAttribute, KnownSymbolNames.WorkleapDomainEventPropagationAbstractionsAssembly)!;

            var domainEventPropagationSymbols = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            domainEventPropagationSymbols.AddIfNotNull(this._domainEventType);
            domainEventPropagationSymbols.AddIfNotNull(this._domainEventAttributeType);

            this._domainEventTypes = domainEventPropagationSymbols.ToImmutable();
        }

        public bool IsValid => this._domainEventTypes.Count == 2 &&
                               this._domainEventType is not null &&
                               this._domainEventAttributeType is not null;

        public void AnalyzeOperationInvocation(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } classTypeSymbol)
            {
                if (this.IsDomainEventClass(type))
                {
                    var hasDomainEventAttribute = type.GetAttributes()
                        .Any(x => x.AttributeClass != null && SymbolEqualityComparer.Default.Equals(x.AttributeClass, this._domainEventAttributeType));

                    if (!hasDomainEventAttribute)
                    {
                        context.ReportDiagnostic(UseDomainEventAttribute, type);
                    }
                }
            }
        }

        private bool IsDomainEventClass(ITypeSymbol type)
        {
            if (this.ImplementsBaseDomainEventInterface(type))
            {
                return true;
            }

            return false;
        }

        private bool ImplementsBaseDomainEventInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this._domainEventType));
        }
    }
}