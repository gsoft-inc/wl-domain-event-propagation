using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Workleap.DomainEventPropagation.Analyzers.Internals;

namespace Workleap.DomainEventPropagation.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventDomainAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor UseDomainEventAttribute = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseDomainEventAttribute,
        title: "Use DomainEvent attribute on event",
        messageFormat: "Use the DomainEvent attribute",
        category: RuleCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    internal static readonly DiagnosticDescriptor UseUniqueNameAttribute = new DiagnosticDescriptor(
        id: RuleIdentifiers.UseUniqueNameForAttributeValue,
        title: "Use unique event name in attribute",
        messageFormat: "Use unique event name in attribute",
        category: RuleCategories.Usage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.HelpUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        UseDomainEventAttribute, UseUniqueNameAttribute);

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
        private readonly INamedTypeSymbol? _domainEventInterfaceType;
        private readonly INamedTypeSymbol? _domainEventAttributeType;
        private readonly ConcurrentDictionary<string, bool> _existingAttributes = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public AnalyzerImplementation(Compilation compilation)
        {
            this._domainEventInterfaceType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventInterface, KnownSymbolNames.WorkleapDomainEventPropagationAbstractionsAssembly)!;
            this._domainEventAttributeType = compilation.GetBestTypeByMetadataName(KnownSymbolNames.DomainEventAttribute, KnownSymbolNames.WorkleapDomainEventPropagationAbstractionsAssembly)!;

            var domainEventPropagationSymbols = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            domainEventPropagationSymbols.AddIfNotNull(this._domainEventInterfaceType);
            domainEventPropagationSymbols.AddIfNotNull(this._domainEventAttributeType);
        }

        public bool IsValid => this._domainEventInterfaceType is not null &&
                               this._domainEventAttributeType is not null;

        public void AnalyzeOperationInvocation(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsAbstract: false } classTypeSymbol)
            {
                if (this.ImplementsBaseDomainEventInterface(classTypeSymbol))
                {
                    var domainEventAttribute = classTypeSymbol.GetAttributes()
                        .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, this._domainEventAttributeType));

                    if (domainEventAttribute is null)
                    {
                        context.ReportDiagnostic(UseDomainEventAttribute, classTypeSymbol);
                    }
                    else if (domainEventAttribute.ConstructorArguments.Length == 1)
                    {
                        var attributeArgument = domainEventAttribute.ConstructorArguments[0].Value;
                        if (attributeArgument is string attributeArgumentString)
                        {
                            var wasAdded = this._existingAttributes.TryAdd(attributeArgumentString, true);

                            if (!wasAdded)
                            {
                                context.ReportDiagnostic(UseUniqueNameAttribute, classTypeSymbol);
                            }
                        }
                    }
                }
            }
        }

        private bool ImplementsBaseDomainEventInterface(ITypeSymbol type)
        {
            return type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this._domainEventInterfaceType));
        }
    }
}