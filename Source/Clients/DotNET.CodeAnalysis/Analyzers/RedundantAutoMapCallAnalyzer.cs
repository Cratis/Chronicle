// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeDefine, SyntaxKind.MethodDeclaration);
    }

    static void AnalyzeDefine(SyntaxNodeAnalysisContext context)
    {
        var declaration = (MethodDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not IMethodSymbol method ||
            method.Name != "Define" ||
            method.Parameters.Length != 1 ||
            method.ContainingType is null)
        {
            return;
        }

        var projection = method.ContainingType.AllInterfaces.FirstOrDefault(_ => _.OriginalDefinition.ToDisplayString() == "Cratis.Chronicle.Projections.IProjectionFor<TReadModel>");
        if (projection is null || projection.TypeArguments[0] is not INamedTypeSymbol readModel ||
            !SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(projection.GetMembers("Define").Single()), method) ||
            method.Parameters[0].Type is not INamedTypeSymbol builderType ||
            builderType.OriginalDefinition.ToDisplayString() != "Cratis.Chronicle.Projections.IProjectionBuilderFor<TReadModel>" ||
            !SymbolEqualityComparer.Default.Equals(builderType.TypeArguments[0], readModel))
        {
            return;
        }

        var noAutoMap = context.Compilation.GetTypeByMetadataName("Cratis.Chronicle.Projections.NoAutoMapAttribute");
        if (noAutoMap is null || HasNoAutoMap(readModel, noAutoMap))
        {
            return;
        }

        var parameters = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { method.Parameters[0] };
        var roots = new Dictionary<ExpressionSyntax, IParameterSymbol?>();
        var identifiers = new List<IdentifierNameSyntax>();
        var calls = new List<InvocationExpressionSyntax>();
        var hasNoAutoMapCall = false;

        // Inspect the entire Define body, including all callback depths, once. Runtime children
        // inherit the parent's final AutoMap state, not its state when the callback is declared.
        foreach (var node in declaration.DescendantNodes())
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation when invocation.Expression is MemberBindingExpressionSyntax binding &&
                    binding.Name.Identifier.Text == "NoAutoMap" && invocation.ArgumentList.Arguments.Count == 0:
                    hasNoAutoMapCall = true;
                    break;
                case InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax access:
                    if (access.Name.Identifier.Text == "NoAutoMap" && invocation.ArgumentList.Arguments.Count == 0)
                    {
                        hasNoAutoMapCall = true;
                    }
                    else if (access.Name.Identifier.Text == AutoMapMethodName && invocation.ArgumentList.Arguments.Count == 0 &&
                        context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol calledMethod && IsProjectionBuilderMethod(calledMethod))
                    {
                        calls.Add(invocation);
                    }
                    break;
                case SimpleLambdaExpressionSyntax lambda:
                    AddCallbackParameter(lambda, lambda.Parameter, context.SemanticModel, parameters, roots);
                    break;
                case ParenthesizedLambdaExpressionSyntax lambda:
                    foreach (var parameter in lambda.ParameterList.Parameters)
                    {
                        AddCallbackParameter(lambda, parameter, context.SemanticModel, parameters, roots);
                    }
                    break;
                case IdentifierNameSyntax identifier:
                    identifiers.Add(identifier);
                    break;
            }
        }

        if (hasNoAutoMapCall || calls.Count == 0 || parameters.OfType<IParameterSymbol>().Any(_ =>
            _.Type is INamedTypeSymbol type && (type.Name == "IChildrenBuilder" || type.Name == "INestedBuilder") &&
            type.TypeArguments.Length > 1 && type.TypeArguments[1] is INamedTypeSymbol child && HasNoAutoMap(child, noAutoMap)))
        {
            return;
        }

        // An alias, assignment, or escape through a non-builder call could change the
        // builder without a visible NoAutoMap call in this method.
        var unsafeUses = new Dictionary<SyntaxNode, bool>();
        if (identifiers.Exists(identifier => context.SemanticModel.GetSymbolInfo(identifier).Symbol is { } symbol &&
            parameters.Contains(symbol) && !IsSafeBuilderUse(identifier, context.SemanticModel, unsafeUses)))
        {
            return;
        }

        foreach (var invocation in calls)
        {
            var access = (MemberAccessExpressionSyntax)invocation.Expression;
            if (GetRootParameter(access.Expression, context.SemanticModel, roots) is not { } root || !parameters.Contains(root))
            {
                continue;
            }

            var location = Location.Create(
                invocation.SyntaxTree,
                TextSpan.FromBounds(access.OperatorToken.SpanStart, invocation.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }

    static bool HasNoAutoMap(INamedTypeSymbol model, INamedTypeSymbol attribute)
    {
        for (var type = model; type is not null; type = type.BaseType)
        {
            if (type.GetAttributes().Any(_ => SymbolEqualityComparer.Default.Equals(_.AttributeClass, attribute)))
            {
                return true;
            }
        }

        return false;
    }

    static void AddCallbackParameter(AnonymousFunctionExpressionSyntax lambda, ParameterSyntax parameter, SemanticModel semanticModel, HashSet<ISymbol> parameters, Dictionary<ExpressionSyntax, IParameterSymbol?> roots)
    {
        if (lambda.Parent is not ArgumentSyntax argument ||
            argument.Parent?.Parent is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax access ||
            semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            !IsProjectionBuilderMethod(method) ||
            GetRootParameter(access.Expression, semanticModel, roots) is not { } root || !parameters.Contains(root) ||
            semanticModel.GetDeclaredSymbol(parameter) is not IParameterSymbol symbol ||
            symbol.Type is not INamedTypeSymbol type ||
            type.Name is not ("IProjectionBuilderFor" or "IFromBuilder" or "IJoinBuilder" or "IChildrenBuilder" or "INestedBuilder"))
        {
            return;
        }

        parameters.Add(symbol);
    }

    static bool IsSafeBuilderUse(IdentifierNameSyntax identifier, SemanticModel semanticModel, Dictionary<SyntaxNode, bool> unsafeUses)
    {
        // Builder parameters must only be used as receivers in fluent calls. Passing a
        // chain's result to another method also lets that method mutate the builder.
        if (HasUnsafeAncestor(identifier, semanticModel, unsafeUses))
        {
            return false;
        }

        var expression = (ExpressionSyntax)identifier;
        while (expression.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized;
        }

        return expression.Parent is MemberAccessExpressionSyntax access && access.Expression == expression &&
            access.Parent is InvocationExpressionSyntax invocation &&
            semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol builderMethod && IsProjectionBuilderMethod(builderMethod);
    }

    static bool HasUnsafeAncestor(SyntaxNode node, SemanticModel semanticModel, Dictionary<SyntaxNode, bool> unsafeUses)
    {
        if (unsafeUses.TryGetValue(node, out var unsafeUse))
        {
            return unsafeUse;
        }

        unsafeUse = (node is AssignmentExpressionSyntax or EqualsValueClauseSyntax or ReturnStatementSyntax) ||
            (node is ArgumentSyntax argument && argument.Parent?.Parent is InvocationExpressionSyntax call &&
                (semanticModel.GetSymbolInfo(call).Symbol is not IMethodSymbol method || !IsProjectionBuilderMethod(method))) ||
            (node.Parent is not null && HasUnsafeAncestor(node.Parent, semanticModel, unsafeUses));
        unsafeUses.Add(node, unsafeUse);

        return unsafeUse;
    }

    static IParameterSymbol? GetRootParameter(ExpressionSyntax expression, SemanticModel semanticModel, Dictionary<ExpressionSyntax, IParameterSymbol?> roots)
    {
        if (roots.TryGetValue(expression, out var root))
        {
            return root;
        }

        root = expression switch
        {
            ParenthesizedExpressionSyntax parenthesized => GetRootParameter(parenthesized.Expression, semanticModel, roots),
            InvocationExpressionSyntax invocation when invocation.Expression is MemberAccessExpressionSyntax access &&
                semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsProjectionBuilderMethod(method) =>
                GetRootParameter(access.Expression, semanticModel, roots),
            IdentifierNameSyntax identifier => semanticModel.GetSymbolInfo(identifier).Symbol as IParameterSymbol,
            _ => null
        };
        roots.Add(expression, root);

        return root;
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
