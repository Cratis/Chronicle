// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a redundant <c language="csharp">[Key]</c> attribute placed on a property or record
/// positional parameter whose type derives from <c language="csharp">EventSourceId&lt;T&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeyOrSubjectOnEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.KeyOrSubjectOnEventSourceId,
        title: "[Key] on an EventSourceId<T> is redundant",
        messageFormat: "'{0}' is typed as '{1}', which derives from EventSourceId<T> and already is the key/stream identity. Remove the [{2}] attribute.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A property or record parameter whose type derives from EventSourceId<T> already is the key (stream identity). Remove the redundant [Key] attribute. An explicit [Subject] can identify a different compliance subject and is not redundant based on its type.");

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

        // For a positional record, [Key] without an explicit target lands on the constructor
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
            if (attribute.AttributeClass?.ToDisplayString() != WellKnownTypes.KeyAttributeName)
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
                "Key"));
        }
    }
}
