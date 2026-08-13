// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a model-bound clear declaration that projection construction never applies.
/// </summary>
/// <remarks>
/// Two shapes are inert today, both of them silently. A <c>[SetValue&lt;TEvent&gt;(null)]</c> is skipped where
/// the set-value mappings are built, so no mapping is emitted at all; and a <c>[ClearWith&lt;TEvent&gt;]</c> is
/// only ever read from the class-level attributes of a nested type, so an application to a property or a
/// parameter binds to nothing.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InertClearDeclarationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The reason given for a null <c>[SetValue]</c>.
    /// </summary>
    internal const string NullSetValueReason = "a null value is skipped when the set-value mappings are built, so no mapping is emitted for the member at all";

    /// <summary>
    /// The reason given for a member-level <c>[ClearWith]</c>.
    /// </summary>
    internal const string MemberClearWithReason = "[ClearWith] is only read from the class-level attributes of a nested single-object type, never from a property or a parameter";

    /// <summary>
    /// The descriptor for the diagnostic.
    /// </summary>
    /// <remarks>
    /// A warning rather than an error for its first release, following the precedent recorded on
    /// <see cref="DiagnosticIds.KeyRedirectionPii"/>: both shapes compile in code that is shipping today, and a
    /// new error would break those builds outright. Neither shape has a correct reading, so raising this to
    /// <see cref="DiagnosticSeverity.Error"/> is defensible later, through a separately reviewed rollout.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.InertClearDeclaration,
        title: "A clear declaration is never applied by projection construction",
        messageFormat: "'{0}' declares a clear that projection construction never applies: {1}. The projected value keeps whatever it last held, including across a full replay, and nothing fails at build time or at registration.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle has no scalar clear. A null [SetValue] is dropped where the projection's set-value mappings are built, yet the member still counts as explicitly mapped, which also suppresses the missing-mapping-source diagnostic. A [ClearWith] on a property or a parameter is read by nothing: the only consumer collects class-level [ClearWith] attributes from a nested type. Both declarations look like a supported clear, compile, register, and leave the stale value standing.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol constructor)
        {
            return;
        }

        var attributeName = constructor.ContainingType.OriginalDefinition.ToDisplayString();

        if (attributeName == WellKnownTypes.SetValueAttributeName && DeclaresNullValue(context, attribute))
        {
            Report(context, attribute, NullSetValueReason);
        }
        else if (attributeName == WellKnownTypes.ClearWithAttributeName && IsOnMember(attribute))
        {
            Report(context, attribute, MemberClearWithReason);
        }
    }

    static void Report(SyntaxNodeAnalysisContext context, AttributeSyntax attribute, string reason) =>
        context.ReportDiagnostic(Diagnostic.Create(Rule, attribute.GetLocation(), attribute.ToString(), reason));

    /// <summary>
    /// Determine whether the single <c>[SetValue]</c> argument is a compile-time null.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="attribute">The attribute application.</param>
    /// <returns>True when the declared value is null.</returns>
    /// <remarks>
    /// Only a value the compiler itself resolves to null is reported. Anything the compiler cannot fold is left
    /// alone, because the attribute argument would not compile as a constant in the first place.
    /// </remarks>
    static bool DeclaresNullValue(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
    {
        if (attribute.ArgumentList?.Arguments is not { Count: > 0 } arguments ||
            arguments[0].NameEquals is not null)
        {
            return false;
        }

        var constant = context.SemanticModel.GetConstantValue(arguments[0].Expression);
        return constant is { HasValue: true, Value: null };
    }

    /// <summary>
    /// Determine whether an attribute is applied to a property or a parameter rather than to a type.
    /// </summary>
    /// <param name="attribute">The attribute application.</param>
    /// <returns>True when the target is a member.</returns>
    static bool IsOnMember(AttributeSyntax attribute) =>
        attribute.Parent is AttributeListSyntax { Parent: PropertyDeclarationSyntax or ParameterSyntax };
}
