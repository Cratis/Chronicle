// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a <c>[PII]</c> attribute placed on a property or record positional parameter
/// whose type derives from <c>EventSourceId&lt;T&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PiiOnEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.PiiOnEventSourceId,
        title: "[PII] cannot be applied to an EventSourceId<T>",
        messageFormat: "'{0}' is typed as '{1}' (derives from EventSourceId<T>); [PII] cannot be applied to an event source id — Chronicle throws PIINotSupportedOnEventSourceId at runtime. Remove [PII].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An EventSourceId<T> is the encryption-key lookup identity, so Chronicle cannot itself encrypt it; marking it [PII] throws PIINotSupportedOnEventSourceId at runtime. The id is already the compliance subject. If the identifier itself is sensitive, use a random Guid-backed surrogate as the event source id and store the sensitive value in a separate [PII] property.");

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

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            ReportForMember(context, property, property.Type);
        }

        // For a positional record, [PII] without an explicit target lands on the constructor
        // parameter rather than the generated property, so the parameters must be inspected too.
        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.Parameters)
            {
                ReportForMember(context, parameter, parameter.Type);
            }
        }
    }

    static void ReportForMember(SymbolAnalysisContext context, ISymbol member, ITypeSymbol memberType)
    {
        if (!WellKnownTypes.DerivesFromEventSourceId(memberType))
        {
            return;
        }

        var attribute = member.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.PiiAttributeName);

        if (attribute is null)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
            ?? member.Locations.FirstOrDefault();

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            member.Name,
            memberType.Name));
    }
}
