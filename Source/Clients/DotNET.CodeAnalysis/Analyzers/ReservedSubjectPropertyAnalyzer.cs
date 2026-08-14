// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a read model declaring a property whose name Chronicle reserves for compliance subjects.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReservedSubjectPropertyAnalyzer : DiagnosticAnalyzer
{
    static readonly string[] _reservedNames = ["_subject", "__subject", "__subjects"];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ReservedSubjectProperty,
        title: "Read model declares a reserved compliance subject property",
        messageFormat: "Read model '{0}' declares a property named '{1}', which Chronicle reserves for internal compliance subject tracking. Rename it.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle reserves its subject fields in stored read model documents for internal compliance-subject tracking. A read-model property with the same name silently collides with that internal field. Rename the property to something else.");

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

        if (!WellKnownTypes.HasAttribute(typeSymbol, WellKnownTypes.ReadModelAttributeName))
        {
            return;
        }

        var reservedProperty = typeSymbol.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(property => !property.IsStatic && _reservedNames.Contains(property.Name));

        if (reservedProperty is not null)
        {
            // For a positional record the generated property's location points at the parameter, so
            // reporting on the property alone covers both the property and the positional parameter.
            Report(context, typeSymbol, reservedProperty.Name, reservedProperty.Locations.FirstOrDefault());
            return;
        }

        // A primary-constructor parameter without a generated property (a class-based read model)
        // still collides with the reserved field.
        var reservedParameter = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault()?.Parameters
            .FirstOrDefault(parameter => _reservedNames.Contains(parameter.Name));

        if (reservedParameter is not null)
        {
            Report(context, typeSymbol, reservedParameter.Name, reservedParameter.Locations.FirstOrDefault());
        }
    }

    static void Report(SymbolAnalysisContext context, INamedTypeSymbol typeSymbol, string propertyName, Location? location) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, typeSymbol.Name, propertyName));
}
