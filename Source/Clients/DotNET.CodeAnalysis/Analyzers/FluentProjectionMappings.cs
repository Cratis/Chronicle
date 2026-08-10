// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reads how a fluent projection block moves event values into its resolved read-model document.
/// </summary>
static class FluentProjectionMappings
{
    const string AddMethodName = "Add";
    const string AddChildMethodName = "AddChild";
    const string AutoMapMethodName = "AutoMap";
    const string ChildrenMethodName = "Children";
    const string FromObjectMethodName = "FromObject";
    const string NestedMethodName = "Nested";
    const string NoAutoMapMethodName = "NoAutoMap";
    const string SetMethodName = "Set";
    const string SetThisValueMethodName = "SetThisValue";
    const string SubtractMethodName = "Subtract";
    const string ToMethodName = "To";
    const string WithMethodName = "With";

    /// <summary>
    /// Enumerate every public fluent mapping that can carry event content into the document.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the callback body.</param>
    /// <param name="builderCallback">The <c>From</c> or <c>Join</c> callback.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <param name="readModelType">The read model being filled by the block.</param>
    /// <param name="eventType">The event being consumed by the block.</param>
    /// <returns>Each mapping as a target property path and an event property path.</returns>
    /// <remarks>
    /// This deliberately follows the semantic builder surface rather than looking for method names. It covers
    /// typed and <c>PropertyPath</c> Set/To, SetThisValue, and Add/Subtract. AddChild creates a separate child
    /// projection operation and is deliberately analyzed through <see cref="GetAddChildMappings"/> instead. Nested member
    /// accessors retain their complete path so PII reach can be evaluated at the actual source leaf.
    /// </remarks>
    internal static IEnumerable<(string TargetName, string EventPropertyName)> GetExplicitMappings(
        SemanticModel semanticModel,
        SyntaxNode builderCallback,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol readModelType,
        INamedTypeSymbol eventType)
    {
        foreach (var invocation in builderCallback.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            {
                continue;
            }

            if (method.Name == ToMethodName &&
                (FluentProjectionSymbols.IsMethodOn(method, symbols.SetBuilder) ||
                 FluentProjectionSymbols.IsMethodOn(method, symbols.TypedSetBuilder)) &&
                TryGetSetMapping(semanticModel, invocation, symbols, readModelType, eventType, out var setMapping))
            {
                yield return setMapping;
                continue;
            }

            if (method.Name == WithMethodName &&
                (FluentProjectionSymbols.IsMethodOn(method, symbols.AddBuilder) ||
                 FluentProjectionSymbols.IsMethodOn(method, symbols.SubtractBuilder)) &&
                TryGetAddOrSubtractMapping(semanticModel, invocation, symbols, readModelType, eventType, out var arithmeticMapping))
            {
                yield return arithmeticMapping;
            }
        }
    }

    /// <summary>
    /// Determine the final AutoMap state that applies at a From or Join invocation.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="invocation">The From or Join invocation.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <returns>True when the owning builder's final AutoMap state is on.</returns>
    internal static bool AutoMapIsOn(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol readModelType,
        FluentProjectionSymbols symbols)
    {
        var owner = GetReceiverIdentity(context.SemanticModel, invocation, symbols);
        if (owner.IsDefault)
        {
            // A method-result receiver whose forwarding behavior cannot be proven must not suppress this
            // compliance diagnostic. Its builder state is unknown, so preserve AutoMap's safe upper bound.
            return true;
        }

        if (GetOwningScope(context.SemanticModel, invocation, owner[0]) is { } scope)
        {
            var finalOverride = (bool?)null;
            foreach (var candidate in scope.DescendantNodesAndSelf()
                         .OfType<InvocationExpressionSyntax>()
                         .Where(candidate => ReceiverIdentitiesEqual(
                             GetReceiverIdentity(context.SemanticModel, candidate, symbols),
                             owner))
                         .OrderBy(candidate => candidate.SpanStart)
                         .ThenBy(candidate => candidate.Span.Length))
            {
                if (context.SemanticModel.GetSymbolInfo(candidate).Symbol is not IMethodSymbol method ||
                    !FluentProjectionSymbols.IsMethodOn(method, symbols.ProjectionBuilder))
                {
                    continue;
                }

                if (method.Name == NoAutoMapMethodName)
                {
                    finalOverride = false;
                }
                else if (method.Name == AutoMapMethodName)
                {
                    finalOverride = true;
                }
            }

            if (finalOverride.HasValue)
            {
                return finalOverride.Value;
            }
        }

        if (WellKnownTypes.HasAttribute(readModelType, WellKnownTypes.NoAutoMapAttributeName))
        {
            return false;
        }

        if (GetEnclosingProjectionScope(context, invocation, symbols) is { } parentInvocation &&
            context.SemanticModel.GetSymbolInfo(parentInvocation).Symbol is IMethodSymbol parentMethod &&
            parentMethod.ContainingType.TypeArguments.FirstOrDefault() is INamedTypeSymbol parentReadModelType)
        {
            return AutoMapIsOn(context, parentInvocation, parentReadModelType, symbols);
        }

        return true;
    }

    /// <summary>
    /// Resolve whether a fluent block writes within a child document and the outer model that owns persistence.
    /// </summary>
    /// <param name="context">The syntax analysis context.</param>
    /// <param name="invocation">The From or Join invocation.</param>
    /// <param name="readModelType">The immediate model filled by the invocation.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <returns>The effective child scope and persisted document model.</returns>
    internal static (bool IsChildScope, INamedTypeSymbol DocumentReadModelType) ResolveStoredDocumentScope(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol readModelType,
        FluentProjectionSymbols symbols)
    {
        var isChildScope = false;
        var documentReadModelType = readModelType;

        foreach (var anonymousFunction in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
        {
            if (anonymousFunction.Parent is not ArgumentSyntax
                {
                    Parent: ArgumentListSyntax
                    {
                        Parent: InvocationExpressionSyntax parentInvocation
                    }
                } ||
                context.SemanticModel.GetSymbolInfo(parentInvocation).Symbol is not IMethodSymbol parentMethod ||
                !FluentProjectionSymbols.IsMethodOn(parentMethod, symbols.ProjectionBuilder) ||
                parentMethod.Name is not ChildrenMethodName and not NestedMethodName ||
                parentMethod.ContainingType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol parentReadModelType)
            {
                continue;
            }

            isChildScope |= parentMethod.Name == ChildrenMethodName;
            documentReadModelType = parentReadModelType;
        }

        return (isChildScope, documentReadModelType);
    }

    /// <summary>
    /// Read a complete member path from an expression accessor or a compile-time string PropertyPath.
    /// </summary>
    /// <param name="semanticModel">The semantic model.</param>
    /// <param name="expression">The accessor or PropertyPath expression.</param>
    /// <param name="path">The resolved path.</param>
    /// <returns>True when the public builder can resolve this source statically.</returns>
    internal static bool TryGetPropertyPath(SemanticModel semanticModel, ExpressionSyntax expression, out string path)
    {
        path = string.Empty;

        if (semanticModel.GetConstantValue(expression) is { HasValue: true, Value: string constant })
        {
            path = constant;
            return constant.Length > 0;
        }

        if (expression is ObjectCreationExpressionSyntax creation &&
            creation.ArgumentList is { Arguments.Count: 1 } argumentList &&
            semanticModel.GetTypeInfo(creation).Type?.ToDisplayString() == "Cratis.Chronicle.Properties.PropertyPath" &&
            semanticModel.GetConstantValue(argumentList.Arguments[0].Expression) is
            {
                HasValue: true,
                Value: string constructedPath
            })
        {
            path = constructedPath;
            return constructedPath.Length > 0;
        }

        var accessor = expression switch
        {
            SimpleLambdaExpressionSyntax { Body: ExpressionSyntax body } simple => (simple.Parameter, body),
            ParenthesizedLambdaExpressionSyntax
            {
                ParameterList.Parameters.Count: 1,
                Body: ExpressionSyntax body
            } parenthesized => (parenthesized.ParameterList.Parameters[0], body),
            _ => default((ParameterSyntax Parameter, ExpressionSyntax Body)?)
        };

        if (accessor is not { } resolvedAccessor)
        {
            return false;
        }

        var current = resolvedAccessor.Body;
        var segments = new Stack<string>();

        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            if (semanticModel.GetSymbolInfo(memberAccess).Symbol is not IPropertySymbol and not IFieldSymbol)
            {
                return false;
            }

            segments.Push(memberAccess.Name.Identifier.Text);
            current = memberAccess.Expression;
        }

        if (current is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != resolvedAccessor.Parameter.Identifier.Text ||
            segments.Count == 0)
        {
            return false;
        }

        path = string.Join(".", segments);
        return true;
    }

    /// <summary>
    /// Enumerate mappings belonging to one AddChild operation.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the AddChild invocation.</param>
    /// <param name="invocation">The AddChild invocation.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <param name="eventType">The event being consumed by the containing From block.</param>
    /// <param name="autoMapIsOn">Whether AutoMap is effective for the child operation.</param>
    /// <param name="resolvedCallback">The callback body when the second argument is a callback.</param>
    /// <returns>Each child mapping as the containing collection and event property path.</returns>
    internal static IEnumerable<(string TargetName, string EventPropertyName)> GetAddChildMappings(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol eventType,
        bool autoMapIsOn,
        FluentProjectionCallback? resolvedCallback = null) =>
        GetAddChildMappingsCore(semanticModel, invocation, symbols, eventType, autoMapIsOn, resolvedCallback);

    static bool TryGetSetMapping(
        SemanticModel semanticModel,
        InvocationExpressionSyntax toInvocation,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol readModelType,
        INamedTypeSymbol eventType,
        out (string TargetName, string EventPropertyName) mapping)
    {
        mapping = default;

        if (toInvocation.ArgumentList.Arguments.Count != 1 ||
            toInvocation.Expression is not MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax setInvocation } ||
            semanticModel.GetSymbolInfo(setInvocation).Symbol is not IMethodSymbol setMethod ||
            !FluentProjectionSymbols.IsMethodOn(setMethod, symbols.ReadModelPropertiesBuilder) ||
            !BuilderMatches(setMethod.ContainingType, readModelType, eventType) ||
            !TryGetPropertyPath(semanticModel, toInvocation.ArgumentList.Arguments[0].Expression, out var source))
        {
            return false;
        }

        string target;
        if (setMethod.Name == SetThisValueMethodName)
        {
            target = readModelType.Name;
        }
        else if (setMethod.Name != SetMethodName ||
                 setInvocation.ArgumentList.Arguments.Count != 1 ||
                 !TryGetPropertyPath(semanticModel, setInvocation.ArgumentList.Arguments[0].Expression, out target))
        {
            return false;
        }

        mapping = (target, source);
        return true;
    }

    static bool TryGetAddOrSubtractMapping(
        SemanticModel semanticModel,
        InvocationExpressionSyntax withInvocation,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol readModelType,
        INamedTypeSymbol eventType,
        out (string TargetName, string EventPropertyName) mapping)
    {
        mapping = default;

        if (withInvocation.ArgumentList.Arguments.Count != 1 ||
            withInvocation.Expression is not MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax operationInvocation } ||
            semanticModel.GetSymbolInfo(operationInvocation).Symbol is not IMethodSymbol operationMethod ||
            !FluentProjectionSymbols.IsMethodOn(operationMethod, symbols.ReadModelPropertiesBuilder) ||
            operationMethod.Name is not AddMethodName and not SubtractMethodName ||
            !BuilderMatches(operationMethod.ContainingType, readModelType, eventType) ||
            operationInvocation.ArgumentList.Arguments.Count != 1 ||
            !TryGetPropertyPath(semanticModel, operationInvocation.ArgumentList.Arguments[0].Expression, out var target) ||
            !TryGetPropertyPath(semanticModel, withInvocation.ArgumentList.Arguments[0].Expression, out var source))
        {
            return false;
        }

        mapping = (target, source);
        return true;
    }

    static IEnumerable<(string TargetName, string EventPropertyName)> GetAddChildMappingsCore(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        FluentProjectionSymbols symbols,
        INamedTypeSymbol eventType,
        bool autoMapIsOn,
        FluentProjectionCallback? resolvedCallback = null)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.Name != AddChildMethodName ||
            !FluentProjectionSymbols.IsMethodOn(method, symbols.ReadModelPropertiesBuilder) ||
            invocation.ArgumentList.Arguments.Count != 2 ||
            !TryGetPropertyPath(semanticModel, invocation.ArgumentList.Arguments[0].Expression, out var target))
        {
            yield break;
        }

        var sourceOrCallback = invocation.ArgumentList.Arguments[1].Expression;
        if (TryGetPropertyPath(semanticModel, sourceOrCallback, out var directSource))
        {
            yield return (target, directSource);
            yield break;
        }

        var mappingBody = resolvedCallback?.Body ?? sourceOrCallback;
        var mappingSemanticModel = resolvedCallback?.SemanticModel ?? semanticModel;
        foreach (var fromObject in mappingBody.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (mappingSemanticModel.GetSymbolInfo(fromObject).Symbol is IMethodSymbol fromObjectMethod &&
                fromObjectMethod.Name == FromObjectMethodName &&
                FluentProjectionSymbols.IsMethodOn(fromObjectMethod, symbols.AddChildBuilder) &&
                fromObject.ArgumentList.Arguments.Count == 1 &&
                TryGetPropertyPath(mappingSemanticModel, fromObject.ArgumentList.Arguments[0].Expression, out var nestedSource))
            {
                yield return (target, nestedSource);
            }
        }

        if (!autoMapIsOn ||
            method.TypeArguments.FirstOrDefault() is not INamedTypeSymbol childModelType ||
            WellKnownTypes.HasAttribute(childModelType, WellKnownTypes.NoAutoMapAttributeName))
        {
            yield break;
        }

        var excludedNames = CrossSubjectPiiJoin.GetMembers(childModelType)
            .Where(member => member.Attributes.Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == WellKnownTypes.NoAutoMapAttributeName))
            .Select(member => member.Name)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        var childMemberNames = CrossSubjectPiiJoin.GetMembers(childModelType)
            .Select(member => member.Name)
            .Where(name => !excludedNames.Contains(name))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var eventMemberName in CrossSubjectPiiJoin.GetMembers(eventType)
                     .Select(member => member.Name)
                     .Where(childMemberNames.Contains)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return (target, eventMemberName);
        }
    }

    static ImmutableArray<ISymbol> GetReceiverIdentity(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        FluentProjectionSymbols symbols)
    {
        var receiver = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;

        while (receiver is not null)
        {
            switch (receiver)
            {
                case InvocationExpressionSyntax methodResult:
                    if (TryResolveForwardedArgument(semanticModel, methodResult, out var forwardedArgument))
                    {
                        receiver = forwardedArgument;
                        continue;
                    }

                    if (semanticModel.GetSymbolInfo(methodResult).Symbol is IMethodSymbol method &&
                        IsProjectionScopeBuilderMethod(method, symbols) &&
                        methodResult.Expression is MemberAccessExpressionSyntax invocationMember)
                    {
                        receiver = invocationMember.Expression;
                        continue;
                    }

                    return default;
                case MemberAccessExpressionSyntax memberAccess:
                    if (IsProjectionBuilderType(semanticModel.GetTypeInfo(memberAccess).Type, symbols))
                    {
                        return GetStableExpressionIdentity(semanticModel, memberAccess);
                    }

                    receiver = memberAccess.Expression;
                    continue;
                case ParenthesizedExpressionSyntax parenthesized:
                    receiver = parenthesized.Expression;
                    continue;
                default:
                    return GetStableExpressionIdentity(semanticModel, receiver);
            }
        }

        return default;
    }

    static bool IsProjectionScopeBuilderMethod(IMethodSymbol method, FluentProjectionSymbols symbols)
        => IsProjectionBuilderType(method.ContainingType, symbols);

    static bool IsProjectionBuilderType(ITypeSymbol? type, FluentProjectionSymbols symbols)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, symbols.ProjectionBuilder) ||
               namedType.AllInterfaces.Any(@interface =>
                   SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, symbols.ProjectionBuilder));
    }

    static ImmutableArray<ISymbol> GetStableExpressionIdentity(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            var member = semanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (member is null)
            {
                return default;
            }

            if (memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax ||
                (member.IsStatic && semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol is INamedTypeSymbol))
            {
                return ImmutableArray.Create(member);
            }

            var owner = GetStableExpressionIdentity(semanticModel, memberAccess.Expression);
            return owner.IsDefault ? default : owner.Add(member);
        }

        if (expression is ThisExpressionSyntax or BaseExpressionSyntax)
        {
            return semanticModel.GetEnclosingSymbol(expression.SpanStart)?.ContainingType is { } containingType
                ? ImmutableArray.Create<ISymbol>(containingType)
                : default;
        }

        return expression is IdentifierNameSyntax && semanticModel.GetSymbolInfo(expression).Symbol is { } symbol
            ? ImmutableArray.Create(symbol)
            : default;
    }

    static bool ReceiverIdentitiesEqual(ImmutableArray<ISymbol> left, ImmutableArray<ISymbol> right)
    {
        if (left.IsDefault || right.IsDefault || left.Length != right.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Length; index++)
        {
            if (!SymbolEqualityComparer.Default.Equals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    static bool TryResolveForwardedArgument(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out ExpressionSyntax forwardedArgument)
    {
        forwardedArgument = null!;

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.DeclaringSyntaxReferences.Length == 0 ||
            GetReturnedParameterOrdinal(method) is not { } parameterOrdinal ||
            semanticModel.GetOperation(invocation) is not IInvocationOperation operation)
        {
            return false;
        }

        if (operation.Arguments
                .FirstOrDefault(argument => argument.Parameter?.Ordinal == parameterOrdinal)
                ?.Value.Syntax is not ExpressionSyntax argumentExpression)
        {
            return false;
        }

        // The argument is a strict syntax descendant of this invocation, so repeated uses of the same
        // forwarding method remain bounded by the finite receiver expression rather than by method identity.
        forwardedArgument = argumentExpression;
        return true;
    }

    static int? GetReturnedParameterOrdinal(IMethodSymbol method)
    {
        foreach (var syntaxReference in method.DeclaringSyntaxReferences)
        {
            var declaration = syntaxReference.GetSyntax();
            var returnedExpression = declaration switch
            {
                MethodDeclarationSyntax { ExpressionBody.Expression: { } expression } => expression,
                MethodDeclarationSyntax { Body.Statements.Count: 1 } methodDeclaration =>
                    (methodDeclaration.Body.Statements[0] as ReturnStatementSyntax)?.Expression,
                LocalFunctionStatementSyntax { ExpressionBody.Expression: { } expression } => expression,
                LocalFunctionStatementSyntax { Body.Statements.Count: 1 } localFunction =>
                    (localFunction.Body.Statements[0] as ReturnStatementSyntax)?.Expression,
                _ => null
            };

            while (returnedExpression is ParenthesizedExpressionSyntax parenthesized)
            {
                returnedExpression = parenthesized.Expression;
            }

            if (returnedExpression is IdentifierNameSyntax identifier &&
                method.Parameters.FirstOrDefault(parameter => parameter.Name == identifier.Identifier.Text) is { } parameter)
            {
                return parameter.Ordinal;
            }
        }

        return null;
    }

    static SyntaxNode? GetOwningScope(SemanticModel semanticModel, InvocationExpressionSyntax invocation, ISymbol owner)
    {
        foreach (var anonymousFunction in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
        {
            if (GetParameters(anonymousFunction).Any(parameter =>
                    SymbolEqualityComparer.Default.Equals(semanticModel.GetDeclaredSymbol(parameter), owner)))
            {
                return anonymousFunction switch
                {
                    SimpleLambdaExpressionSyntax simple => simple.Body,
                    ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.Body,
                    AnonymousMethodExpressionSyntax anonymous => anonymous.Block,
                    _ => null
                };
            }
        }

        return invocation.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
    }

    static SeparatedSyntaxList<ParameterSyntax> GetParameters(AnonymousFunctionExpressionSyntax anonymousFunction) =>
        anonymousFunction switch
        {
            SimpleLambdaExpressionSyntax simple => [simple.Parameter],
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters,
            AnonymousMethodExpressionSyntax { ParameterList: not null } anonymous => anonymous.ParameterList.Parameters,
            _ => []
        };

    static InvocationExpressionSyntax? GetEnclosingProjectionScope(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        FluentProjectionSymbols symbols)
    {
        foreach (var anonymousFunction in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
        {
            if (anonymousFunction.Parent is not ArgumentSyntax
                {
                    Parent: ArgumentListSyntax
                    {
                        Parent: InvocationExpressionSyntax parentInvocation
                    }
                } ||
                ReferenceEquals(parentInvocation, invocation) ||
                context.SemanticModel.GetSymbolInfo(parentInvocation).Symbol is not IMethodSymbol parentMethod ||
                !FluentProjectionSymbols.IsMethodOn(parentMethod, symbols.ProjectionBuilder) ||
                parentMethod.Name is not ChildrenMethodName and not NestedMethodName)
            {
                continue;
            }

            return parentInvocation;
        }

        return null;
    }

    static bool BuilderMatches(INamedTypeSymbol builder, INamedTypeSymbol readModelType, INamedTypeSymbol eventType) =>
        builder.TypeArguments.Length >= 2 &&
        SymbolEqualityComparer.Default.Equals(builder.TypeArguments[0], readModelType) &&
        SymbolEqualityComparer.Default.Equals(builder.TypeArguments[1], eventType);
}
