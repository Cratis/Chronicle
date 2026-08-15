// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a clear declared for a read model member that cannot hold null.
/// </summary>
/// <remarks>
/// Clearing means returning a member to no value. A member declared non-nullable has no such state, so the
/// declaration has no correct reading: the only thing a projection could write is the type default - an empty
/// string, a zero - which is a different fact that the read model cannot tell apart from a real value. Standing a
/// type default in for "not set" is exactly the sentinel-leaking the scalar clear exists to remove, so the
/// declaration is refused rather than reinterpreted.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ClearOnNonNullableMemberAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The descriptor for the diagnostic.
    /// </summary>
    /// <remarks>
    /// An error rather than a warning. A scalar clear is new capability, so no shipping code can be relying on this
    /// declaration working - until now both spellings were reported by the retired <c>CHR0047</c> as inert, and the
    /// third (the fluent <c>ToValue(null)</c>) silently wrote the string "null". There is nothing to break and no
    /// reading under which the declaration is what its author meant.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ClearOnNonNullableMember,
        title: "A clear is declared for a member that cannot hold null",
        messageFormat: "'{0}' clears '{1}', which is declared as '{2}' and cannot hold null. Declare the member as nullable, or use [SetValue<TEvent>(...)] with the value you actually want it to hold.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Clearing a read model member means returning it to no value. A member declared non-nullable has no such state, so the only thing the projection could write is the type default - an empty string, a zero - which is a different fact the read model cannot tell apart from a real value. Declare the member as nullable to clear it, or set the value explicitly with [SetValue<TEvent>(...)].");

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
        var declaresClear = attributeName switch
        {
            WellKnownTypes.ClearWithAttributeName => true,
            WellKnownTypes.SetValueAttributeName => DeclaresNullValue(context, attribute),
            _ => false
        };

        if (!declaresClear)
        {
            return;
        }

        var memberType = GetMemberType(context, attribute);
        if (memberType is null || CanHoldNull(memberType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            attribute.GetLocation(),
            attribute.ToString(),
            GetMemberName(attribute),
            memberType.ToDisplayString()));
    }

    /// <summary>
    /// Determine whether the single <c>[SetValue]</c> argument is a compile-time null.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="attribute">The attribute application.</param>
    /// <returns>True when the declared value is null.</returns>
    /// <remarks>
    /// Only a value the compiler itself resolves to null is treated as a clear, so a cast null and a null-forgiving
    /// null are caught alongside the bare one, and anything the compiler cannot fold is left alone - it would not
    /// compile as an attribute argument in the first place.
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
    /// Resolve the declared type of the member the attribute is applied to.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="attribute">The attribute application.</param>
    /// <returns>The member's type, or <see langword="null"/> when the attribute is not on a member.</returns>
    /// <remarks>
    /// A class-level <c>[ClearWith]</c> on a nested type has no member type and is never a scalar clear, so it
    /// resolves to nothing and is never reported.
    /// </remarks>
    static ITypeSymbol? GetMemberType(SyntaxNodeAnalysisContext context, AttributeSyntax attribute) =>
        attribute.Parent is not AttributeListSyntax list
            ? null
            : list.Parent switch
            {
                PropertyDeclarationSyntax property => context.SemanticModel.GetDeclaredSymbol(property)?.Type,
                ParameterSyntax parameter => context.SemanticModel.GetDeclaredSymbol(parameter)?.Type,
                _ => null
            };

    static string GetMemberName(AttributeSyntax attribute) =>
        attribute.Parent is AttributeListSyntax list
            ? list.Parent switch
            {
                PropertyDeclarationSyntax property => property.Identifier.Text,
                ParameterSyntax parameter => parameter.Identifier.Text,
                _ => string.Empty
            }
            : string.Empty;

    /// <summary>
    /// Determine whether a member's declared type can hold null.
    /// </summary>
    /// <param name="type">The declared type of the member.</param>
    /// <returns>True when the member can hold null, false when it cannot.</returns>
    /// <remarks>
    /// A reference type in a file that has opted out of nullable analysis is oblivious rather than non-null, so it
    /// is left alone - the declaration is the author's to make and the projection builder agrees. A value type that
    /// is not <c>Nullable&lt;T&gt;</c> cannot hold null whatever the nullable context says.
    /// </remarks>
    static bool CanHoldNull(ITypeSymbol type) =>
        type.IsValueType
            ? type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            : type.NullableAnnotation != NullableAnnotation.NotAnnotated;
}
