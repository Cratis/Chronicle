// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Validates visible self-reducer read-model builder ownership.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReadModelBuilderOwnershipAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = CreateRule(DiagnosticSeverity.Warning);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var builders = new Lazy<ILookup<ITypeSymbol, INamedTypeSymbol>>(() => IndexBuilders(start.Compilation, start.CancellationToken));
            start.RegisterSymbolAction(symbol => Analyze(symbol, builders), SymbolKind.NamedType);
        });
    }

    static DiagnosticDescriptor CreateRule(DiagnosticSeverity severity) => new(
        DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous,
        "Read model builder ownership must be unambiguous",
        "Self reducer '{0}' also has another visible builder: '{1}'",
        "Usage",
        severity,
        isEnabledByDefault: true);

    static void Analyze(SymbolAnalysisContext context, Lazy<ILookup<ITypeSymbol, INamedTypeSymbol>> index)
    {
        var model = (INamedTypeSymbol)context.Symbol;
        if (!SelfReducerPolicy.IsSelf(model, context.Compilation)) return;
        if (HasConflictingOwnership(model, context.Compilation, index.Value[model]))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, model.Locations.FirstOrDefault(), DiagnosticSeverity.Error, null, null, model.Name, model.Name));
            return;
        }
        foreach (var builder in index.Value[model].Where(type => !SelfReducerPolicy.Equal(type, model)))
        {
            // Different clients can own different registrations; a second declaration does not prove a conflict.
            context.ReportDiagnostic(Diagnostic.Create(Rule, model.Locations.FirstOrDefault(), model.Name, builder.Name));
        }
    }

    static bool HasConflictingOwnership(INamedTypeSymbol model, Compilation compilation, IEnumerable<INamedTypeSymbol> builders) =>
        WellKnownTypes.ImplementsIReactor(model, compilation) || HasMapping(model) || builders.Any(builder => SelfReducerPolicy.Equal(builder, model));

    static ILookup<ITypeSymbol, INamedTypeSymbol> IndexBuilders(Compilation compilation, CancellationToken cancellationToken)
    {
        var symbols = WellKnownSymbols.For(compilation);
        var reducer = symbols.Get(WellKnownTypes.IReducerForName);
        var projection = symbols.Get(WellKnownTypes.IProjectionForName);
        return SelfReducerPolicy.Types(compilation.Assembly.GlobalNamespace)
            .SelectMany(builder => Targets(builder, reducer, projection, cancellationToken).Select(model => (Model: model, Builder: builder)))
            .ToLookup(pair => pair.Model, pair => pair.Builder, (IEqualityComparer<ITypeSymbol>)SymbolEqualityComparer.Default);
    }

    static IEnumerable<ITypeSymbol> Targets(INamedTypeSymbol builder, INamedTypeSymbol? reducer, INamedTypeSymbol? projection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return builder.AllInterfaces.Where(contract => contract.IsGenericType &&
                (SelfReducerPolicy.Equal(contract.OriginalDefinition, reducer) || SelfReducerPolicy.Equal(contract.OriginalDefinition, projection)))
            .Select(contract => contract.TypeArguments[0]).Distinct<ITypeSymbol>(SymbolEqualityComparer.Default);
    }

    static IEnumerable<AttributeData> PropertyAttributes(INamedTypeSymbol model)
    {
        for (var current = model; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetMembers().OfType<IPropertySymbol>().Where(property => property.DeclaredAccessibility == Accessibility.Public).SelectMany(property => property.GetAttributes()))
            {
                yield return attribute;
            }
        }
    }

    static bool HasMapping(INamedTypeSymbol model) =>
        SelfReducerPolicy.Attributes(model).Concat(PropertyAttributes(model))
            .Concat(model.InstanceConstructors.SelectMany(constructor => constructor.Parameters).SelectMany(parameter => parameter.GetAttributes()))
            .Any(attribute => attribute.AttributeClass?.ToDisplayString() != WellKnownTypes.PassiveAttributeName &&
                attribute.AttributeClass?.AllInterfaces.Any(contract => contract.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound.IProjectionAnnotation") == true);
}
