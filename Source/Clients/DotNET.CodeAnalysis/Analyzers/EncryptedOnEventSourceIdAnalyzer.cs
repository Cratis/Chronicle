// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an <c language="csharp">[Encrypted]</c> attribute placed on a property or record positional
/// parameter whose type derives from <c language="csharp">EventSourceId&lt;T&gt;</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EncryptedOnEventSourceIdAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.EncryptedOnEventSourceId,
        title: "[Encrypted] cannot be applied to an EventSourceId<T>",
        messageFormat: "'{0}' is typed as '{1}' (derives from EventSourceId<T>); [Encrypted] cannot be applied to an event source id — Chronicle throws EncryptedNotSupportedOnEventSourceId at runtime. Remove [Encrypted].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An EventSourceId<T> is used to correlate events and cannot be encrypted; marking it [Encrypted] throws EncryptedNotSupportedOnEventSourceId at runtime. If the identifier itself is a secret, use a non-sensitive surrogate as the event source id and store the secret in a separate [Encrypted] property.");

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

        // For a positional record, [Encrypted] without an explicit target lands on the constructor
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
            .FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.EncryptedAttributeName);

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
