// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
    /// A warning for its first release, following the precedent recorded on the retired <c>CHR0047</c>: these
    /// analyzers ship inside the <c>Cratis.Chronicle</c> package, so under <c>TreatWarningsAsErrors</c> a new
    /// warning already breaks a consumer build on upgrade - which is what makes it a minor release. An error is a
    /// strictly stronger break and buys little here, because the declaration never worked in the first place.
    /// The declaration has no correct reading, so this is scheduled to become
    /// <see cref="DiagnosticSeverity.Error"/> in the next major.
    /// </remarks>
    internal static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.ClearOnNonNullableMember,
        title: "A clear is declared for a member that cannot hold null",
        messageFormat: "'{0}' clears '{1}', which is declared as '{2}' and cannot hold null. Declare the member as nullable, or use [SetValue<TEvent>(...)] with the value you actually want it to hold.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Clearing a read model member means returning it to no value. A member declared non-nullable has no such state, so the only thing the projection could write is the type default - an empty string, a zero - which is a different fact the read model cannot tell apart from a real value. Building the projection refuses the declaration outright, so leaving this warning unaddressed fails at startup rather than at build time. Declare the member as nullable to clear it, or set the value explicitly with [SetValue<TEvent>(...)].");

    const string ClearMethodName = "Clear";
    const string ToValueMethodName = "ToValue";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var symbols = FluentProjectionSymbols.TryCreate(compilationStart.Compilation);
            if (symbols is null)
            {
                return;
            }

            compilationStart.RegisterSyntaxNodeAction(syntaxNode => AnalyzeInvocation(syntaxNode, symbols), SyntaxKind.InvocationExpression);
        });
    }

    /// <summary>
    /// Analyze the fluent spellings of a clear.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="symbols">The resolved Chronicle fluent builder symbols.</param>
    /// <remarks>
    /// C# cannot express "a nullable-annotated reference type" as a generic constraint - a non-nullable argument
    /// converts to a nullable parameter without complaint - so <c>Clear</c> cannot refuse this at its signature and
    /// the rule has to be applied here. <c>Set(...).ToValue(null)</c> is the same clear and is held to the same rule.
    /// </remarks>
    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, FluentProjectionSymbols symbols)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return;
        }

        var (memberType, memberName) = method switch
        {
            { Name: ClearMethodName } when FluentProjectionSymbols.IsMethodOn(method, symbols.ReadModelPropertiesBuilder) =>
                GetClearedMember(context, invocation),
            { Name: ToValueMethodName } when FluentProjectionSymbols.IsMethodOn(method, symbols.TypedSetBuilder) && DeclaresNullArgument(context, invocation) =>
                (method.ContainingType.TypeArguments[2], GetSetMemberName(invocation)),
            _ => (null, string.Empty)
        };

        if (memberType is null || CanHoldNull(memberType))
        {
            return;
        }

        var call = GetCallSpan(invocation);

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            Location.Create(invocation.SyntaxTree, call),
            invocation.SyntaxTree.GetText(context.CancellationToken).ToString(call),
            memberName,
            memberType.ToDisplayString()));
    }

    /// <summary>
    /// Narrow an invocation to the call itself, excluding whatever it is chained onto.
    /// </summary>
    /// <param name="invocation">The invocation to narrow.</param>
    /// <returns>The span covering the method name and its arguments.</returns>
    /// <remarks>
    /// A fluent call's syntax node starts at the receiver, so reporting the node underlines the whole chain up to
    /// this point. The offending declaration is the call, so that is what gets the squiggle.
    /// </remarks>
    static TextSpan GetCallSpan(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax memberAccess
            ? TextSpan.FromBounds(memberAccess.Name.SpanStart, invocation.Span.End)
            : invocation.Span;

    /// <summary>
    /// Resolve the member a <c>Clear</c> call targets from its accessor lambda.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="invocation">The <c>Clear</c> invocation.</param>
    /// <returns>The member's type and name, or a null type when the accessor is not a plain property access.</returns>
    static (ITypeSymbol? Type, string Name) GetClearedMember(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0 ||
            invocation.ArgumentList.Arguments[0].Expression is not SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess })
        {
            return (null, string.Empty);
        }

        return context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is IPropertySymbol property
            ? (property.Type, property.Name)
            : (null, string.Empty);
    }

    /// <summary>
    /// Determine whether a <c>ToValue</c> call declares a compile-time null, which makes it a clear.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="invocation">The <c>ToValue</c> invocation.</param>
    /// <returns>True when the argument folds to null.</returns>
    static bool DeclaresNullArgument(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation) =>
        invocation.ArgumentList.Arguments.Count > 0 &&
        context.SemanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression) is { HasValue: true, Value: null };

    /// <summary>
    /// Recover the member name from the <c>Set(...)</c> that a <c>ToValue</c> continues, for the message only.
    /// </summary>
    /// <param name="invocation">The <c>ToValue</c> invocation.</param>
    /// <returns>The member name, or an empty string when it cannot be read off the chain.</returns>
    static string GetSetMemberName(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax setInvocation } &&
        setInvocation.ArgumentList.Arguments.Count > 0 &&
        setInvocation.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax setMember }
            ? setMember.Name.Identifier.Text
            : string.Empty;

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
