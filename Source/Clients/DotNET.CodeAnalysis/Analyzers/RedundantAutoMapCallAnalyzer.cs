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
/// Analyzer that reports an explicit <c>.AutoMap()</c> call on a projection builder as redundant,
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

        if (!IsProjectionBuilderMethod(methodSymbol))
        {
            return;
        }

        // Report on the '.AutoMap()' portion only, so a chained expression is not flagged in its entirety.
        var location = Location.Create(
            invocation.SyntaxTree,
            TextSpan.FromBounds(memberAccess.OperatorToken.SpanStart, invocation.Span.End));

        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
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
