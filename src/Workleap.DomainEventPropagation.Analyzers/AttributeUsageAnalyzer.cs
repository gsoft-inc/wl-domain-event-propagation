using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Workleap.DomainEventPropagation.Analyzers.Internals;
using Workleap.Extensions.MediatR.Analyzers.Internals;

namespace Workleap.DomainEventPropagation.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeUsageAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseGenericParameterRule = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseDomainEventAttribute,
        title: "Use DomainEvent attribute on event",
        messageFormat: "Use the DomainEvent attribute",
        category: RuleCategories.Design,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseGenericParameterRule);

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
        private readonly INamedTypeSymbol _domainEventType;
        private readonly ImmutableHashSet<INamedTypeSymbol> _domainEventTypes;
        private readonly INamedTypeSymbol _domainEventAttributeType;

        public AnalyzerImplementation(Compilation compilation)
        {
            this._domainEventType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventInterface, KnownSymbolNames.DomainEventInterface)!;
            this._domainEventAttributeType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventAttribute, KnownSymbolNames.DomainEventInterface)!;

            var domainEventPropagationSymbols = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            domainEventPropagationSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventInterface, KnownSymbolNames.DomainEventInterface));
            domainEventPropagationSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventAttribute, KnownSymbolNames.DomainEventAttribute));

            this._domainEventTypes = domainEventPropagationSymbols.ToImmutable();
        }

        public bool IsValid => this._domainEventTypes.Count == 1;

        public void AnalyzeOperationInvocation(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } type)
            {
                if (this.IsDomainEventClass(context, type))
                {
                    var hasDomainEventAttribute = type.ContainingType.GetAttributes()
                        .Any(x => x.AttributeClass != null && SymbolEqualityComparer.Default.Equals(x.AttributeClass, this._domainEventAttributeType));

                    var targetAttribute = context.Compilation.GetTypeByMetadataName(KnownSymbolNames.DomainEventAttribute);
                    if (targetAttribute is null)
                    {
                        context.ReportDiagnostic(UseGenericParameterRule, type);
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