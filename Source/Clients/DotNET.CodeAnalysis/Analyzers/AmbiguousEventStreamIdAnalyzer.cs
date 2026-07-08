// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a type that both implements <c>ICanProvideEventStreamId</c> and declares a non-null
/// <c>[EventStreamId]</c> attribute, which throws <c>AmbiguousEventStreamId</c> at startup.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AmbiguousEventStreamIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AmbiguousEventStreamId,
        title: "Ambiguous event stream id",
        messageFormat: "'{0}' both implements ICanProvideEventStreamId and declares a non-null [EventStreamId]; this throws AmbiguousEventStreamId at startup. Remove one — use [EventStreamId(null)] to defer to the interface.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A type that implements ICanProvideEventStreamId supplies the event stream id dynamically at runtime. Declaring a non-null [EventStreamId] attribute on the same type conflicts with that interface and Chronicle throws AmbiguousEventStreamId when it starts. Remove either the interface or the attribute; the sanctioned [EventStreamId(null)] defers to the interface and is allowed.");

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

        if (!ImplementsICanProvideEventStreamId(typeSymbol, context.Compilation))
        {
            return;
        }

        var attribute = typeSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == WellKnownTypes.EventStreamIdAttributeName);

        if (attribute is null || !HasNonNullValue(attribute))
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
            ?? typeSymbol.Locations.FirstOrDefault();

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            typeSymbol.Name));
    }

    static bool ImplementsICanProvideEventStreamId(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var interfaceSymbol = compilation.GetTypeByMetadataName(WellKnownTypes.ICanProvideEventStreamIdName);
        return interfaceSymbol is not null && typeSymbol.AllInterfaces.Contains(interfaceSymbol, SymbolEqualityComparer.Default);
    }

    static bool HasNonNullValue(AttributeData attribute)
    {
        // The EventStreamId attribute's first constructor argument is the stream id value. A missing or
        // null/empty value means the sanctioned defer to the interface, so only a non-empty string conflicts.
        if (attribute.ConstructorArguments.Length == 0)
        {
            return false;
        }

        return attribute.ConstructorArguments[0].Value is string value && value.Length > 0;
    }
}
