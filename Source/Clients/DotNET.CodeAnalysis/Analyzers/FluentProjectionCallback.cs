// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// A same-compilation fluent projection callback together with the semantic model for its body.
/// </summary>
readonly struct FluentProjectionCallback
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FluentProjectionCallback"/> struct.
    /// </summary>
    /// <param name="body">The callback body.</param>
    /// <param name="semanticModel">The semantic model for the body.</param>
    internal FluentProjectionCallback(SyntaxNode body, SemanticModel semanticModel)
    {
        Body = body;
        SemanticModel = semanticModel;
    }

    /// <summary>Gets the callback body.</summary>
    internal SyntaxNode Body { get; }

    /// <summary>Gets the semantic model for the callback body.</summary>
    internal SemanticModel SemanticModel { get; }

    /// <summary>
    /// Resolve an inline lambda, local delegate initializer, field delegate initializer, local function, or method group.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="expression">The callback expression.</param>
    /// <param name="callback">The resolved callback body and semantic model.</param>
    /// <returns>True when the callback is available in the current compilation.</returns>
    internal static bool TryResolve(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax expression,
        out FluentProjectionCallback callback) =>
        TryResolve(context.Compilation, context.SemanticModel, expression, new(SymbolEqualityComparer.Default), out callback);

    /// <summary>
    /// Resolve a callback discovered inside another resolved callback body.
    /// </summary>
    /// <param name="compilation">The containing compilation.</param>
    /// <param name="semanticModel">The semantic model for the callback expression.</param>
    /// <param name="expression">The callback expression.</param>
    /// <param name="callback">The resolved callback body and semantic model.</param>
    /// <returns>True when the callback is available in the current compilation.</returns>
    internal static bool TryResolve(
        Compilation compilation,
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        out FluentProjectionCallback callback) =>
        TryResolve(compilation, semanticModel, expression, new(SymbolEqualityComparer.Default), out callback);

    static bool TryResolve(
        Compilation compilation,
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        HashSet<ISymbol> visited,
        out FluentProjectionCallback callback)
    {
        expression = expression is ParenthesizedExpressionSyntax parenthesized
            ? parenthesized.Expression
            : expression;

        if (expression is AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            callback = new(anonymousFunction, semanticModel);
            return true;
        }

        if (expression is ObjectCreationExpressionSyntax
            {
                ArgumentList.Arguments.Count: 1
            } delegateCreation)
        {
            return TryResolve(
                compilation,
                semanticModel,
                delegateCreation.ArgumentList.Arguments[0].Expression,
                visited,
                out callback);
        }

        if (semanticModel.GetSymbolInfo(expression).Symbol is not { } symbol || !visited.Add(symbol))
        {
            callback = default;
            return false;
        }

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var declaration = syntaxReference.GetSyntax();
            var body = declaration switch
            {
                VariableDeclaratorSyntax { Initializer.Value: ExpressionSyntax initializer } => (SyntaxNode)initializer,
                MethodDeclarationSyntax { Body: not null } method => method.Body,
                MethodDeclarationSyntax { ExpressionBody.Expression: { } expressionBody } => expressionBody,
                LocalFunctionStatementSyntax { Body: not null } localFunction => localFunction.Body,
                LocalFunctionStatementSyntax { ExpressionBody.Expression: { } expressionBody } => expressionBody,
                _ => null
            };

            if (body is null)
            {
                continue;
            }

            var declarationSemanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
            if (body is ExpressionSyntax bodyExpression &&
                (symbol is ILocalSymbol or IFieldSymbol || bodyExpression is AnonymousFunctionExpressionSyntax) &&
                TryResolve(compilation, declarationSemanticModel, bodyExpression, visited, out callback))
            {
                return true;
            }

            callback = new(body, declarationSemanticModel);
            return true;
        }

        callback = default;
        return false;
    }
}
