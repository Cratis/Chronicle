// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Chronicle.CodeAnalysis.Analyzers;

/// <summary>
/// The fluent builder methods that decide which document a projected event lands on.
/// </summary>
/// <remarks>
/// Shared by every analyzer that has to tell a document re-key from a child identity, so that the set of
/// method names and the scope rule are stated exactly once.
/// </remarks>
static class KeyRedirection
{
    /// <summary>Keys the root document by a value read from the event.</summary>
    internal const string UsingKey = nameof(UsingKey);

    /// <summary>Keys the root document by a member of the event context.</summary>
    internal const string UsingKeyFromContext = nameof(UsingKeyFromContext);

    /// <summary>Keys the parent document by a value read from the event.</summary>
    internal const string UsingParentKey = nameof(UsingParentKey);

    /// <summary>Keys the parent document by a member of the event context.</summary>
    internal const string UsingParentKeyFromContext = nameof(UsingParentKeyFromContext);

    /// <summary>Keys the root document by a composite built from several event values.</summary>
    internal const string UsingCompositeKey = nameof(UsingCompositeKey);

    /// <summary>Keys the parent document by a composite built from several event values.</summary>
    internal const string UsingParentCompositeKey = nameof(UsingParentCompositeKey);

    /// <summary>Pins the root document to a constant key.</summary>
    internal const string UsingConstantKey = nameof(UsingConstantKey);

    /// <summary>Pins the parent document to a constant key.</summary>
    internal const string UsingConstantParentKey = nameof(UsingConstantParentKey);

    /// <summary>
    /// Every key-redirection method, root and parent alike.
    /// </summary>
    internal static readonly ImmutableHashSet<string> All = ImmutableHashSet.Create(
        UsingKey,
        UsingKeyFromContext,
        UsingParentKey,
        UsingParentKeyFromContext,
        UsingCompositeKey,
        UsingParentCompositeKey,
        UsingConstantKey,
        UsingConstantParentKey);

    /// <summary>
    /// The methods that re-key the root document a block writes to.
    /// </summary>
    internal static readonly ImmutableHashSet<string> Root = ImmutableHashSet.Create(
        UsingKey,
        UsingKeyFromContext,
        UsingCompositeKey,
        UsingConstantKey);

    /// <summary>
    /// The methods that re-key the parent a child block attaches to.
    /// </summary>
    internal static readonly ImmutableHashSet<string> Parent = ImmutableHashSet.Create(
        UsingParentKey,
        UsingParentKeyFromContext,
        UsingParentCompositeKey,
        UsingConstantParentKey);

    /// <summary>
    /// Determine whether an invocation is one of the key-redirection calls belonging to this exact
    /// <c>From&lt;TEvent&gt;</c> block.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the invocation.</param>
    /// <param name="invocation">The invocation to check.</param>
    /// <param name="eventType">The event the block reads.</param>
    /// <param name="readModelType">The read model being projected.</param>
    /// <param name="isChildScope">Whether the From block fills a child inside a containing document.</param>
    /// <param name="symbols">The Chronicle builder symbols.</param>
    /// <returns>True when the call redirects this block's key, false otherwise.</returns>
    /// <remarks>
    /// Matching the declaring interface together with both of its type arguments is what keeps a nested
    /// builder — an <c>AddChild</c> callback, a sibling block written on the same chain — from being read as
    /// this block's own key.
    /// </remarks>
    internal static bool IsDocumentRedirectionFor(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol eventType,
        INamedTypeSymbol readModelType,
        bool isChildScope,
        FluentProjectionSymbols symbols)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            !All.Contains(method.Name))
        {
            return false;
        }

        if (!FluentProjectionSymbols.IsMethodOn(method, symbols.ReadModelPropertiesBuilder) ||
            method.ContainingType.TypeArguments.Length != 3 ||
            !SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[0], readModelType) ||
            !SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[1], eventType))
        {
            return false;
        }

        return isChildScope
            ? Parent.Contains(method.Name)
            : Root.Contains(method.Name);
    }
}
