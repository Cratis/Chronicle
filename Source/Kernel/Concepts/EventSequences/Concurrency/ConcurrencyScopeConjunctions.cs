// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.EventSequences.Concurrency;

/// <summary>
/// Ordered guards which must all hold for an atomic append. This type never uses the legacy dictionary codec.
/// </summary>
/// <param name="entries">The guards, with their positions in the original request.</param>
public class ConcurrencyScopeConjunctions(IEnumerable<(EventSourceId EventSourceId, ConcurrencyScope Scope, uint? ScopeIndex)> entries)
{
    /// <summary>
    /// Gets the ordered guards, including repeated event source identifiers.
    /// </summary>
    public IReadOnlyList<(EventSourceId EventSourceId, ConcurrencyScope Scope, uint? ScopeIndex)> Entries { get; } = entries.ToArray();

    /// <summary>
    /// Gets whether every guard asks for a check the kernel can perform.
    /// </summary>
    public bool ShouldAllBeValidated => Entries.Count > 0 && Entries.All(_ => _.Scope.ShouldBeValidated);

    /// <summary>
    /// Gets whether the guards require the new grain protocol.
    /// </summary>
    public bool HasRepeatedIds => Entries.Select(_ => _.EventSourceId).Distinct().Count() != Entries.Count;
}
