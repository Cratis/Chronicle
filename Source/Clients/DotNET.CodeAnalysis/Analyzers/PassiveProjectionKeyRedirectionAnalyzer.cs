// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a model-bound projection which re-keys the root document of a <b>passive</b> read
/// model, so that a read replays a stream the events were never appended to.
/// </summary>
/// <remarks>
/// The fluent equivalent is covered by <see cref="FluentPassiveProjectionKeyRedirectionAnalyzer"/>; both report
/// <see cref="DiagnosticIds.PassiveProjectionKeyRedirection"/>. Only the root <c>key:</c> is read here.
/// <c>parentKey:</c> identifies a child inside its containing document and never moves the document a passive
/// read resolves.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PassiveProjectionKeyRedirectionAnalyzer : DiagnosticAnalyzer
{
    const string ModelBoundNamespace = "Cratis.Chronicle.Projections.ModelBound";
    const string FromEventAttributeName = "FromEventAttribute";
    const string KeyParameterName = "key";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(PassiveProjectionKeyRedirection.Rule);

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

        if (!WellKnownTypes.HasAttribute(typeSymbol, WellKnownTypes.PassiveAttributeName))
        {
            return;
        }

        foreach (var attribute in typeSymbol.GetAttributes().Where(IsFromEvent))
        {
            if (GetEventType(attribute) is not { } eventType ||
                ReadKey(attribute) is not { } key ||
                attribute.ApplicationSyntaxReference?.GetSyntax() is not { } attributeSyntax)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                PassiveProjectionKeyRedirection.Rule,
                attributeSyntax.GetLocation(),
                PassiveProjectionKeyRedirection.ModelBoundRedirectionDisplayName,
                typeSymbol.Name,
                key,
                eventType.Name));
        }
    }

    static bool IsFromEvent(AttributeData attribute) =>
        attribute.AttributeClass is { } attributeClass &&
        attributeClass.Name == FromEventAttributeName &&
        attributeClass.ContainingNamespace?.ToDisplayString() == ModelBoundNamespace;

    static INamedTypeSymbol? GetEventType(AttributeData attribute) =>
        attribute.AttributeClass?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;

    static string? ReadKey(AttributeData attribute)
    {
        if (attribute.AttributeConstructor is not { } constructor)
        {
            return null;
        }

        var arguments = attribute.ConstructorArguments;
        for (var i = 0; i < constructor.Parameters.Length && i < arguments.Length; i++)
        {
            if (constructor.Parameters[i].Name == KeyParameterName && arguments[i].Value is string value && value.Length > 0)
            {
                return value;
            }
        }

        return null;
    }
}
