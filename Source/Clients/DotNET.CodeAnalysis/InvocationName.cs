// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Reads an invoked method name without binding the invocation.
/// </summary>
internal static class InvocationName
{
    /// <summary>
    /// Gets the invoked identifier, including generic, unqualified and conditional calls.
    /// </summary>
    /// <param name="invocation">The invocation syntax.</param>
    /// <returns>The identifier value, or null for an expression without a method name.</returns>
    internal static string? Of(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax member => member.Name.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => null
    };
}
