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
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    static DiagnosticDescriptor CreateRule(DiagnosticSeverity severity) => new(
        DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous,
        "Read model builder ownership must be unambiguous",
        "Self reducer '{0}' also has another visible builder: '{1}'",
        "Usage",
        severity,
        isEnabledByDefault: true);

    static void Analyze(SymbolAnalysisContext context)
    {
        var model = (INamedTypeSymbol)context.Symbol;
        if (!SelfReducerPolicy.IsSelf(model, context.Compilation)) return;
        if (WellKnownTypes.ImplementsIReactor(model, context.Compilation) || HasMapping(model) || Targets(model, model, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, model.Locations.FirstOrDefault(), DiagnosticSeverity.Error, null, null, model.Name, model.Name));
            return;
        }
        var builders = SelfReducerPolicy.Types(context.Compilation.Assembly.GlobalNamespace).Where(type =>
            !SelfReducerPolicy.Equal(type, model) && Targets(type, model, context.Compilation));
        foreach (var builder in builders)
        {
            // Different clients can own different registrations; a second declaration does not prove a conflict.
            context.ReportDiagnostic(Diagnostic.Create(Rule, model.Locations.FirstOrDefault(), model.Name, builder.Name));
        }
    }

    static bool Targets(INamedTypeSymbol builder, INamedTypeSymbol model, Compilation compilation) => builder.AllInterfaces.Any(contract =>
        (SelfReducerPolicy.Equal(contract.OriginalDefinition, compilation.GetTypeByMetadataName(WellKnownTypes.IReducerForName)) ||
         SelfReducerPolicy.Equal(contract.OriginalDefinition, compilation.GetTypeByMetadataName(WellKnownTypes.IProjectionForName))) &&
        SelfReducerPolicy.Equal(contract.TypeArguments.FirstOrDefault(), model));

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
