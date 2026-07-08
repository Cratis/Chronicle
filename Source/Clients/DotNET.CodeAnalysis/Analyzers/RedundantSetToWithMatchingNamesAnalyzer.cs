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
/// Analyzer that reports a <c>.Set(x =&gt; x.P).To(e =&gt; e.P)</c> mapping on a projection builder as redundant
/// when the source and target property names are identical, since AutoMap already maps identically named properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantSetToWithMatchingNamesAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// The name of the Set builder method.
    /// </summary>
    const string SetMethodName = "Set";

    /// <summary>
    /// The name of the To builder method.
    /// </summary>
    const string ToMethodName = "To";

    static readonly string[] _builderInterfaceNames =
    [
        "IProjectionBuilderFor",
        "IProjectionBuilder",
        "IFromBuilder",
        "IJoinBuilder",
        "IChildrenBuilder",
        "IReadModelPropertiesBuilder",
        "ISetBuilder"
    ];

    static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.RedundantSetToWithMatchingNames,
        title: "Redundant .Set(...).To(...) with matching property names",
        messageFormat: "'.Set(x => x.{0}).To(e => e.{0})' is redundant — AutoMap already maps identically named properties. Remove the mapping.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AutoMap is enabled by default and maps event properties to identically named read model properties. A .Set(x => x.P).To(e => e.P) mapping where the source and target property names are the same duplicates what AutoMap already does. Remove the redundant mapping and rely on AutoMap.");

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
        var toInvocation = (InvocationExpressionSyntax)context.Node;

        // Expect the '.To(<lambda>)' invocation.
        if (toInvocation.Expression is not MemberAccessExpressionSyntax toMember)
        {
            return;
        }

        if (toMember.Name.Identifier.Text != ToMethodName)
        {
            return;
        }

        if (toInvocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        // The receiver of '.To(...)' must be a '.Set(<lambda>)' invocation.
        if (toMember.Expression is not InvocationExpressionSyntax setInvocation)
        {
            return;
        }

        if (setInvocation.Expression is not MemberAccessExpressionSyntax setMember)
        {
            return;
        }

        if (setMember.Name.Identifier.Text != SetMethodName)
        {
            return;
        }

        if (setInvocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(toInvocation).Symbol is not IMethodSymbol toMethod ||
            !IsProjectionBuilderMethod(toMethod))
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(setInvocation).Symbol is not IMethodSymbol setMethod ||
            !IsProjectionBuilderMethod(setMethod))
        {
            return;
        }

        // Be conservative: only fire when both lambdas are simple 'param => param.Member' member accesses.
        if (!TryGetSimpleMemberName(setInvocation.ArgumentList.Arguments[0].Expression, out var setName) ||
            !TryGetSimpleMemberName(toInvocation.ArgumentList.Arguments[0].Expression, out var toName))
        {
            return;
        }

        if (!string.Equals(setName, toName, StringComparison.Ordinal))
        {
            return;
        }

        // Report on the '.Set(...).To(...)' segment.
        var location = Location.Create(
            toInvocation.SyntaxTree,
            TextSpan.FromBounds(setMember.OperatorToken.SpanStart, toInvocation.Span.End));

        context.ReportDiagnostic(Diagnostic.Create(Rule, location, setName));
    }

    static bool TryGetSimpleMemberName(ExpressionSyntax expression, out string name)
    {
        name = string.Empty;

        if (expression is not SimpleLambdaExpressionSyntax lambda)
        {
            return false;
        }

        if (lambda.Body is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Require the access to be directly on the lambda parameter (e.g. x => x.P).
        if (memberAccess.Expression is not IdentifierNameSyntax identifier)
        {
            return false;
        }

        if (identifier.Identifier.Text != lambda.Parameter.Identifier.Text)
        {
            return false;
        }

        name = memberAccess.Name.Identifier.Text;
        return true;
    }

    static bool IsProjectionBuilderMethod(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        var typeName = containingType.OriginalDefinition.ToDisplayString();
        foreach (var builderInterfaceName in _builderInterfaceNames)
        {
            if (typeName.Contains(builderInterfaceName))
            {
                return true;
            }
        }

        return false;
    }
}
