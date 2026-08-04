// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a member carrying several <c>[SetFromContext]</c> for the same event type.
/// </summary>
/// <remarks>
/// The attribute allows multiple deliberately, and that is load-bearing: one property capturing context from
/// several <em>distinct</em> event types is exactly what it is for. Two for the <em>same</em> event type on one
/// member is different - they write the same key into the same definition, so the last declared wins and the
/// earlier is discarded with no build, registration or runtime signal.
/// <para>
/// This is reported where the neighbouring auto-map collision rule deliberately only informs, and the difference
/// is the point: that collision has two legitimate resolutions and only the author knows which was meant, while a
/// scalar cannot hold two context values under any configuration, naming policy or event shape. There is no
/// second reading to defer to.
/// </para>
/// <para>
/// The failure is also maximally quiet. The property is populated - with the other value - so nothing is null,
/// nothing throws, and a spec asserting the property has a value stays green. It reads as a projection written
/// wrong, so re-reading the attributes and finding both present and correctly spelled confirms the wrong
/// conclusion.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DuplicateSetFromContextAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.DuplicateSetFromContextForSameEventType,
        title: "Several [SetFromContext] for the same event type on one member",
        messageFormat: "'{0}' carries more than one [SetFromContext<{1}>], and they map to the same property - only the last declared is kept",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A member can capture context from several different event types, but not several times from the same one - a single value cannot hold two. All but the last declared are silently dropped. Keep the one that was intended, or move the other onto its own property.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        foreach (var member in GetPropertiesAndParameters(typeSymbol))
        {
            AnalyzeMember(context, member);
        }
    }

    static void AnalyzeMember(SymbolAnalysisContext context, ISymbol member)
    {
        var duplicated = member.GetAttributes()
            .Select(EventTypeOfSetFromContext)
            .Where(_ => _ is not null)
            .GroupBy(_ => _, SymbolEqualityComparer.Default)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key);

        foreach (var eventType in duplicated)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                member.Locations.FirstOrDefault(),
                member.Name,
                eventType!.Name));
        }
    }

    static INamedTypeSymbol? EventTypeOfSetFromContext(AttributeData attribute)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass is not { IsGenericType: true, TypeArguments.Length: 1 })
        {
            return null;
        }

        return string.Equals(
            attributeClass.OriginalDefinition.ToDisplayString(),
            WellKnownTypes.SetFromContextAttributeName,
            StringComparison.Ordinal)
            ? attributeClass.TypeArguments[0] as INamedTypeSymbol
            : null;
    }

    static IEnumerable<ISymbol> GetPropertiesAndParameters(INamedTypeSymbol typeSymbol)
    {
        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            yield return property;
        }

        // For a positional record, attributes without an explicit target land on the constructor parameter
        // rather than the generated property, so the parameters must be inspected too. A child record's
        // parameters are reached the same way - the builder has a third copy of the mapping loop for those, so a
        // check that only looked at root read models would miss them.
        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        foreach (var parameter in primaryConstructor?.Parameters ?? [])
        {
            yield return parameter;
        }
    }
}
