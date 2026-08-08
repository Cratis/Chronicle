// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// Reads the two things a fluent projection builder callback says about how an event fills a read model:
/// which properties it maps by hand, and whether AutoMap is still carrying the rest.
/// </summary>
static class FluentProjectionMappings
{
    const string SetMethodName = "Set";
    const string ToMethodName = "To";
    const string NoAutoMapMethodName = "NoAutoMap";

    /// <summary>
    /// Enumerate the <c>.Set(x =&gt; x.P).To(e =&gt; e.Q)</c> mappings written inside a builder callback.
    /// </summary>
    /// <param name="builderCallback">The builder callback to read.</param>
    /// <returns>Each mapping as the read model property and the event property it takes its value from.</returns>
    internal static IEnumerable<(string TargetName, string EventPropertyName)> GetExplicitMappings(ExpressionSyntax builderCallback)
    {
        foreach (var toInvocation in builderCallback.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (toInvocation.Expression is not MemberAccessExpressionSyntax toMember ||
                toMember.Name.Identifier.Text != ToMethodName ||
                toInvocation.ArgumentList.Arguments.Count != 1 ||
                toMember.Expression is not InvocationExpressionSyntax setInvocation ||
                setInvocation.Expression is not MemberAccessExpressionSyntax setMember ||
                setMember.Name.Identifier.Text != SetMethodName ||
                setInvocation.ArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            if (TryGetSimpleMemberName(setInvocation.ArgumentList.Arguments[0].Expression, out var targetName) &&
                TryGetSimpleMemberName(toInvocation.ArgumentList.Arguments[0].Expression, out var eventPropertyName))
            {
                yield return (targetName, eventPropertyName);
            }
        }
    }

    /// <summary>
    /// Determine whether AutoMap is still on for the projection an invocation belongs to.
    /// </summary>
    /// <param name="invocation">The builder invocation under analysis.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <returns>True when AutoMap can carry properties across, false when it was turned off.</returns>
    /// <remarks>
    /// AutoMap is turned off two ways: <c>[NoAutoMap]</c> on the read model itself, and <c>.NoAutoMap()</c> on
    /// the builder. The latter applies to a scope, which is not modelled from syntax — any <c>NoAutoMap</c>
    /// anywhere in the declaring member suppresses the implicit half of the check. Both rules built on this err
    /// towards missing a case over reporting a correct build.
    /// </remarks>
    internal static bool AutoMapIsOn(InvocationExpressionSyntax invocation, INamedTypeSymbol readModelType) =>
        !WellKnownTypes.HasAttribute(readModelType, WellKnownTypes.NoAutoMapAttributeName) &&
        invocation.Ancestors()
            .OfType<MemberDeclarationSyntax>()
            .FirstOrDefault()
            ?.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(member => member.Name.Identifier.Text == NoAutoMapMethodName) != true;

    /// <summary>
    /// Read the property name out of a <c>_ =&gt; _.Property</c> accessor lambda.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="name">The property name when the expression is such an accessor.</param>
    /// <returns>True when the expression is a single member access on the lambda parameter, false otherwise.</returns>
    internal static bool TryGetSimpleMemberName(ExpressionSyntax expression, out string name)
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
