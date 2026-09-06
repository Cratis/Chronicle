// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Defines a system that manages causation.
/// </summary>
public interface ICausationManager
{
    /// <summary>
    /// Gets the root causation.
    /// </summary>
    Causation Root { get; }

    /// <summary>
    /// Gets the current causation.
    /// </summary>
    /// <returns>A collection of <see cref="Causation"/>.</returns>
    IImmutableList<Causation> GetCurrentChain();

    /// <summary>
    /// Adds a causation.
    /// </summary>
    /// <param name="type">Type to add.</param>
    /// <param name="properties">Properties associated with the causation.</param>
    void Add(CausationType type, IDictionary<string, string> properties);

    /// <summary>
    /// Adds a causation that lasts only as long as the returned scope.
    /// </summary>
    /// <param name="type">Type to add.</param>
    /// <param name="properties">Properties associated with the causation.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the causation again.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="Add"/> is append-only, which is right for a link that describes how the work arrived and stays
    /// true for everything that follows - an HTTP request, a reactor invocation. It is wrong for a link that
    /// describes one bounded piece of work, because two such pieces done one after the other both end up on the
    /// chain and the second reads as caused by the first. That is an ordering nothing actually established.
    /// </para>
    /// <para>
    /// Scopes are last-in first-out: disposing one removes its causation and anything added after it. Dispose is
    /// idempotent, and disposing out of order removes the later scopes with it rather than corrupting the chain.
    /// </para>
    /// </remarks>
    IDisposable BeginScope(CausationType type, IDictionary<string, string> properties);
}
