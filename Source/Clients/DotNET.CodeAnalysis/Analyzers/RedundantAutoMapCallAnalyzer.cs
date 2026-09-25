// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports an explicit <c language="csharp">.AutoMap()</c> call on a projection builder as redundant,
/// since AutoMap is enabled by default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantAutoMapCallAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The name of the AutoMap builder method.
    /// </summary>
    const string AutoMapMethodName = "AutoMap";

    static readonly string[] _builderInterfaceNames =
    [
        "IProjectionBuilderFor",
        "IProjectionBuilder",
        "IFromBuilder",
        "IJoinBuilder",
        "IChildrenBuilder"
    ];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.RedundantAutoMapCall,
        title: "Redundant .AutoMap() call",
        messageFormat: "'.AutoMap()' is redundant — AutoMap is enabled by default. Remove the call.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AutoMap is enabled by default on projection builders, so calling .AutoMap() explicitly has no effect. Remove the redundant call. Use .NoAutoMap() when you need to disable the default behavior.");

    enum AutoMapState
    {
        Unknown = 0,
        Enabled = 1,
        Disabled = 2
    }

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        if (memberAccess.Name.Identifier.Text != AutoMapMethodName)
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count != 0)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!IsProjectionBuilderMethod(methodSymbol) || GetState(memberAccess.Expression, invocation, context.SemanticModel) != AutoMapState.Enabled)
        {
            return;
        }

        // Report on the '.AutoMap()' portion only, so a chained expression is not flagged in its entirety.
        var location = Location.Create(
            invocation.SyntaxTree,
            TextSpan.FromBounds(memberAccess.OperatorToken.SpanStart, invocation.Span.End));

        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }

    /// <summary>
    /// Computes the known AutoMap state of a receiver in its builder scope.
    /// </summary>
    /// <param name="receiver">The expression receiving the call.</param>
    /// <param name="call">The call whose incoming state is needed.</param>
    /// <param name="semanticModel">The semantic model for the builder.</param>
    /// <returns>The state, or unknown if it cannot be proven.</returns>
    static AutoMapState GetState(ExpressionSyntax receiver, SyntaxNode call, SemanticModel semanticModel)
    {
        if (receiver is ParenthesizedExpressionSyntax parenthesized)
        {
            return GetState(parenthesized.Expression, call, semanticModel);
        }

        if (receiver is InvocationExpressionSyntax invocation && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            var state = GetState(access.Expression, call, semanticModel);
            if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsProjectionBuilderMethod(method))
            {
                return access.Name.Identifier.Text switch
                {
                    "NoAutoMap" => AutoMapState.Disabled,
                    "AutoMap" => AutoMapState.Enabled,
                    _ => state
                };
            }

            return state;
        }

        if (receiver is not IdentifierNameSyntax identifier || semanticModel.GetSymbolInfo(identifier).Symbol is not IParameterSymbol parameter)
        {
            return AutoMapState.Unknown;
        }

        var stateAtEntry = GetInitialState(parameter, call, semanticModel);
        var block = call.Ancestors().OfType<BlockSyntax>().FirstOrDefault(_ => _.Span.Contains(call.Span));
        if (block is null)
        {
            return stateAtEntry;
        }

        var statement = block.Statements.FirstOrDefault(_ => _.Span.Contains(call.Span));
        if (statement is null)
        {
            return stateAtEntry;
        }

        // Only statements in this builder's own block can precede this call. Do not let
        // NoAutoMap on an unrelated builder (or in a different callback) change its state.
        foreach (var preceding in block.Statements.TakeWhile(_ => _ != statement))
        {
            // Once another name can refer to this mutable builder, syntax on the original
            // parameter cannot establish that AutoMap is still enabled.
            if (preceding.DescendantNodes().OfType<VariableDeclaratorSyntax>().Any(_ =>
                _.Initializer?.Value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name =>
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(name).Symbol, parameter)) == true))
            {
                return AutoMapState.Unknown;
            }

            foreach (var earlier in preceding.DescendantNodes().OfType<InvocationExpressionSyntax>().OrderBy(_ => _.Span.End))
            {
                if (earlier.Expression is not MemberAccessExpressionSyntax earlierAccess ||
                    GetRoot(earlierAccess.Expression) is not IdentifierNameSyntax root ||
                    !SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(root).Symbol, parameter) ||
                    semanticModel.GetSymbolInfo(earlier).Symbol is not IMethodSymbol earlierMethod ||
                    !IsProjectionBuilderMethod(earlierMethod))
                {
                    continue;
                }

                // A conditional/looped configuration call does not establish the state for
                // the next statement, even if the last textual call says AutoMap.
                if ((preceding is not ExpressionStatementSyntax ||
                    earlier.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any(_ => preceding.Span.Contains(_.Span))) &&
                    (earlierAccess.Name.Identifier.Text == "NoAutoMap" || earlierAccess.Name.Identifier.Text == "AutoMap"))
                {
                    return AutoMapState.Unknown;
                }

                stateAtEntry = earlierAccess.Name.Identifier.Text switch
                {
                    "NoAutoMap" => AutoMapState.Disabled,
                    "AutoMap" => AutoMapState.Enabled,
                    _ => stateAtEntry
                };
            }
        }

        return stateAtEntry;
    }

    static ExpressionSyntax GetRoot(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => GetRoot(parenthesized.Expression),
        InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax access => GetRoot(access.Expression),
        _ => expression
    };

    static AutoMapState GetInitialState(IParameterSymbol parameter, SyntaxNode call, SemanticModel semanticModel)
    {
        if (parameter.Type is not INamedTypeSymbol builderType || builderType.TypeArguments.Length == 0)
        {
            return AutoMapState.Unknown;
        }

        // Both the projection and the child model may carry [NoAutoMap]. Runtime initialization
        // gives the attribute precedence over the constructor's enabled/inherited default.
        var isChild = builderType.Name == "IChildrenBuilder" || builderType.Name == "INestedBuilder";
        var modelIndex = isChild ? 1 : 0;
        if (builderType.TypeArguments.Length <= modelIndex)
        {
            return AutoMapState.Unknown;
        }

        if (builderType.TypeArguments[modelIndex].GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "Cratis.Chronicle.Projections.NoAutoMapAttribute"))
        {
            return AutoMapState.Disabled;
        }

        if (builderType.Name == "IProjectionBuilderFor")
        {
            return AutoMapState.Enabled;
        }

        if (!isChild ||
            parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not ParameterSyntax declaration ||
            declaration.Parent is not SimpleLambdaExpressionSyntax lambda ||
            lambda.Parent is not ArgumentSyntax argument ||
            argument.Parent?.Parent is not InvocationExpressionSyntax parentCall ||
            parentCall.Expression is not MemberAccessExpressionSyntax parentAccess ||
            parentAccess.Name.Identifier.Text is not ("Children" or "Nested"))
        {
            return AutoMapState.Unknown;
        }

        // A child is built with AutoMap.Inherit. Its enclosing builder's state is the
        // inherited state until the child overrides it with AutoMap or NoAutoMap.
        return GetState(parentAccess.Expression, parentCall, semanticModel);
    }

    static bool IsProjectionBuilderMethod(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        var typeName = containingType.OriginalDefinition.ToDisplayString();
        return _builderInterfaceNames.Any(builderInterfaceName => typeName.Contains(builderInterfaceName));
    }
}
