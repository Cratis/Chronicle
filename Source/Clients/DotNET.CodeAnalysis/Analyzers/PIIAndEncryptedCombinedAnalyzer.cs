// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a property, record positional parameter, or type carrying both
/// <c language="csharp">[PII]</c> and <c language="csharp">[Encrypted]</c>.
/// </summary>
/// <remarks>
/// Covers the two directly-visible combinations - both attributes on the same member, and one on the member
/// with the other on its declaring type. <c language="csharp">PIIAndEncryptedCombinedNotSupported</c>
/// is thrown at schema-generation time for every combination this static check cannot see (the attribute on a
/// referenced concept type, or on a matching constructor parameter of a different declaring type), so a build
/// without this warning is not proof the combination is absent - only that this specific shape of it is.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PIIAndEncryptedCombinedAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.PIIAndEncryptedCombined,
        title: "[PII] and [Encrypted] cannot both apply to the same value",
        messageFormat: "'{0}' resolves both [PII] and [Encrypted] compliance metadata; a value needs exactly one protection — combining them corrupts it (see PIIAndEncryptedCombinedNotSupported). Remove one.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Chronicle applies every matching compliance handler for a property in sequence, so a value marked both [PII] and [Encrypted] is encrypted twice under two different keys and cannot be released correctly. Choose [PII] for personal data with a lawful basis for erasure, or [Encrypted] for an operational secret with none.");

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
        var typeHasPII = HasAttribute(typeSymbol, WellKnownTypes.PiiAttributeName);
        var typeHasEncrypted = HasAttribute(typeSymbol, WellKnownTypes.EncryptedAttributeName);

        foreach (var property in typeSymbol.GetMembers().OfType<IPropertySymbol>().Where(property => !property.IsStatic))
        {
            ReportForMember(context, property, typeHasPII, typeHasEncrypted);
        }

        var primaryConstructor = typeSymbol.InstanceConstructors
            .OrderByDescending(constructor => constructor.Parameters.Length)
            .FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.Parameters)
            {
                ReportForMember(context, parameter, typeHasPII, typeHasEncrypted);
            }
        }
    }

    static void ReportForMember(SymbolAnalysisContext context, ISymbol member, bool typeHasPII, bool typeHasEncrypted)
    {
        var memberHasPII = HasAttribute(member, WellKnownTypes.PiiAttributeName);
        var memberHasEncrypted = HasAttribute(member, WellKnownTypes.EncryptedAttributeName);

        var hasPII = memberHasPII || typeHasPII;
        var hasEncrypted = memberHasEncrypted || typeHasEncrypted;

        if (!hasPII || !hasEncrypted)
        {
            return;
        }

        // Report on whichever attribute actually sits on this member - falling back to the member's own
        // location when both come from the declaring type (nothing on the member itself to point at).
        var attribute = member.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() is string name &&
            (string.Equals(name, WellKnownTypes.PiiAttributeName, StringComparison.Ordinal) || string.Equals(name, WellKnownTypes.EncryptedAttributeName, StringComparison.Ordinal)));

        var location = attribute?.ApplicationSyntaxReference?.GetSyntax().GetLocation()
            ?? member.Locations.FirstOrDefault();

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, member.Name));
    }

    static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);
}
