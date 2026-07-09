// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a <c>[Key]</c> or <c>[Subject]</c> attribute placed on a property or record
/// positional parameter whose type derives from <c>EventSourceId&lt;T&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeyOrSubjectOnEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.KeyOrSubjectOnEventSourceId,
        title: "[Key] or [Subject] on an EventSourceId<T> is redundant",
        messageFormat: "'{0}' is typed as '{1}', which derives from EventSourceId<T> and already is the key/stream identity and compliance subject. Remove the [{2}] attribute.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property or record parameter whose type derives from EventSourceId<T> already is both the key (stream identity) and the compliance subject. [Key] and [Subject] exist for non-EventSourceId<T> values; on an EventSourceId<T> they are at best redundant and at worst mask an ambiguous multi-identity design that should be split into distinct types rather than annotated around. Remove the attribute.");

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

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic)
            {
                continue;
            }

            ReportForMember(context, property, property.Type);
        }

        // For a positional record, [Key]/[Subject] without an explicit target lands on the constructor
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

        foreach (var attribute in member.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            var displayName = attributeName switch
            {
                WellKnownTypes.KeyAttributeName => "Key",
                WellKnownTypes.SubjectAttributeName => "Subject",
                _ => null
            };

            if (displayName is null)
            {
                continue;
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? member.Locations.FirstOrDefault();

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                location,
                member.Name,
                memberType.Name,
                displayName));
        }
    }
}
