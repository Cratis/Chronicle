// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency scope for an event sequence append many operation.
/// </summary>
/// <param name="scopes">The scopes.</param>
public class ConcurrencyScopes(IDictionary<EventSourceId, ConcurrencyScope> scopes)
{
    /// <summary>
    /// Gets the scopes.
    /// </summary>
    public IDictionary<EventSourceId, ConcurrencyScope> Scopes { get; } = new Dictionary<EventSourceId, ConcurrencyScope>(scopes);

    /// <summary>
    /// Gets a value indicating whether every scope here asks for a concurrency check the kernel can perform.
    /// </summary>
    /// <remarks>
    /// An empty collection is false rather than vacuously true - no scope asked for anything, so nothing was
    /// checked. A caller reading this off the append result wants to know that the guarantee it believes it has
    /// covers the whole batch, and one skipped scope is enough to break that.
    /// </remarks>
    public bool ShouldAllBeValidated => Scopes.Count > 0 && Scopes.Values.All(scope => scope.ShouldBeValidated);

    /// <summary>
    /// Gets the <see cref="ConcurrencyScope"/> for the <see cref="EventSourceId"/>.
    /// If there is no <see cref="ConcurrencyScope"/> then <see cref="ConcurrencyScope.None"/> will be returned.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The <see cref="ConcurrencyScope"/>.</returns>
    public ConcurrencyScope GetFor(EventSourceId eventSourceId) =>
        Scopes.TryGetValue(eventSourceId, out var scope) ? scope : ConcurrencyScope.None;
}
