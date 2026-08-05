// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Analyzer that reports a fluent <c>From&lt;TEvent&gt;</c> block writing a property that a sibling
/// <c>Join&lt;TOther&gt;</c> in the same projection also sets explicitly — a combination where the joined
/// value always wins and the local write can never reset the property.
/// </summary>
/// <remarks>
/// The model-bound equivalent is covered by <see cref="JoinOverridesLocalWriteAnalyzer"/>; both report
/// <see cref="DiagnosticIds.JoinOverridesLocalWrite"/>. Only explicit mappings on both sides are correlated —
/// values a join carries across via AutoMap are not modelled from syntax.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FluentJoinOverridesLocalWriteAnalyzer : DiagnosticAnalyzer
{
    const string FromMethodName = "From";
    const string JoinMethodName = "Join";
    const string SetMethodName = "Set";
    const string ProjectionBuilderInterfaceName = "IProjectionBuilder";
    const string PropertiesBuilderInterfaceName = "IReadModelPropertiesBuilder";

    /// <summary>
    /// The property-builder methods that write a read model property from the block's own event.
    /// </summary>
    static readonly HashSet<string> LocalWriteMethodNames = new(StringComparer.Ordinal)
    {
        SetMethodName,
        "Add",
        "Subtract",
        "Count",
        "Increment",
        "Decrement"
    };

    static readonly HashSet<string> JoinWriteMethodNames = new(StringComparer.Ordinal)
    {
        SetMethodName
    };

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(JoinOverridesLocalWrite.Rule);

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

        if (invocation.Expression is not MemberAccessExpressionSyntax member ||
            member.Name.Identifier.Text != FromMethodName ||
            invocation.ArgumentList.Arguments.Count != 1)
        {
            return;
        }

        if (GetProjectionMethod(context, invocation) is not { } from)
        {
            return;
        }

        var callback = invocation.ArgumentList.Arguments[0].Expression;
        var localWrites = GetWrittenProperties(context, callback, from.ReadModelType, LocalWriteMethodNames).ToArray();
        if (localWrites.Length == 0)
        {
            return;
        }

        var joins = GetSiblingJoins(context, invocation, from.ReadModelType).ToArray();

        foreach (var write in localWrites)
        {
            ReportFirstCollidingJoin(context, write, joins, from.EventType);
        }
    }

    static void ReportFirstCollidingJoin(
        SyntaxNodeAnalysisContext context,
        (string PropertyName, Location Location) write,
        IEnumerable<(INamedTypeSymbol EventType, ImmutableHashSet<string> Properties)> joins,
        INamedTypeSymbol fromEventType)
    {
        var join = joins.FirstOrDefault(join => join.Properties.Contains(write.PropertyName));
        if (join.EventType is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            JoinOverridesLocalWrite.Rule,
            write.Location,
            write.PropertyName,
            $"the From<{fromEventType.Name}> block",
            join.EventType.Name));
    }

    /// <summary>
    /// Find the joins declared alongside a <c>From</c> block for the same read model, with the properties each
    /// sets explicitly.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="fromInvocation">The <c>From</c> invocation being analyzed.</param>
    /// <param name="readModelType">The read model the <c>From</c> block projects into.</param>
    /// <returns>Each sibling join's event type and explicitly set property names.</returns>
    /// <remarks>
    /// Siblings are scoped to the containing member — the <c>Define</c> method — and matched on the read model
    /// type, so a join inside a child scope never collides with a root-level <c>From</c> of the same property
    /// name.
    /// </remarks>
    static IEnumerable<(INamedTypeSymbol EventType, ImmutableHashSet<string> Properties)> GetSiblingJoins(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax fromInvocation,
        INamedTypeSymbol readModelType)
    {
        var containingMember = fromInvocation.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        if (containingMember is null)
        {
            yield break;
        }

        var joinInvocations = containingMember.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text == JoinMethodName &&
                invocation.ArgumentList.Arguments.Count == 1);

        foreach (var invocation in joinInvocations)
        {
            if (GetProjectionMethod(context, invocation) is not { } join ||
                !SymbolEqualityComparer.Default.Equals(join.ReadModelType, readModelType))
            {
                continue;
            }

            var properties = GetWrittenProperties(context, invocation.ArgumentList.Arguments[0].Expression, readModelType, JoinWriteMethodNames)
                .Select(write => write.PropertyName)
                .ToImmutableHashSet(StringComparer.Ordinal);

            if (!properties.IsEmpty)
            {
                yield return (join.EventType, properties);
            }
        }
    }

    /// <summary>
    /// Resolve a <c>From</c>/<c>Join</c> invocation to its event and read model types.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="invocation">The invocation to resolve.</param>
    /// <returns>The event and read model types, or <see langword="null"/> when the call is not a projection builder's.</returns>
    static (INamedTypeSymbol EventType, INamedTypeSymbol ReadModelType)? GetProjectionMethod(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation)
    {
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.ContainingType?.OriginalDefinition.Name != ProjectionBuilderInterfaceName ||
            method.TypeArguments.FirstOrDefault() is not INamedTypeSymbol eventType ||
            method.ContainingType.TypeArguments.FirstOrDefault() is not INamedTypeSymbol readModelType)
        {
            return null;
        }

        return (eventType, readModelType);
    }

    /// <summary>
    /// Find the read model properties a builder callback writes explicitly.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="builderCallback">The <c>From</c> or <c>Join</c> builder callback.</param>
    /// <param name="readModelType">The read model the enclosing block projects into.</param>
    /// <param name="methodNames">The property-writing builder methods to look for.</param>
    /// <returns>Each written property's name and the location of the write.</returns>
    static IEnumerable<(string PropertyName, Location Location)> GetWrittenProperties(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax builderCallback,
        INamedTypeSymbol readModelType,
        HashSet<string> methodNames)
    {
        foreach (var invocation in builderCallback.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member ||
                !methodNames.Contains(member.Name.Identifier.Text) ||
                invocation.ArgumentList.Arguments.Count == 0 ||
                !IsPropertiesBuilderMethodFor(context, invocation, readModelType) ||
                !TryGetSimpleMemberName(invocation.ArgumentList.Arguments[0].Expression, out var propertyName))
            {
                continue;
            }

            yield return (propertyName, invocation.ArgumentList.Arguments[0].Expression.GetLocation());
        }
    }

    static bool IsPropertiesBuilderMethodFor(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, INamedTypeSymbol readModelType) =>
        context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        method.ContainingType?.OriginalDefinition.Name == PropertiesBuilderInterfaceName &&
        method.ContainingType.TypeArguments.FirstOrDefault() is INamedTypeSymbol modelType &&
        SymbolEqualityComparer.Default.Equals(modelType, readModelType);

    static bool TryGetSimpleMemberName(ExpressionSyntax expression, out string name)
    {
        name = string.Empty;

        if (expression is not SimpleLambdaExpressionSyntax lambda ||
            lambda.Body is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Expression is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != lambda.Parameter.Identifier.Text)
        {
            return false;
        }

        name = memberAccess.Name.Identifier.Text;
        return true;
    }
}
