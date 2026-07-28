// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents an <see cref="EqualityComparer{T}"/> for <see cref="EventStoreName"/> that ignores casing.
/// </summary>
/// <remarks>
/// <see cref="IStorage.GetEventStore"/> resolves cached <see cref="IEventStoreStorage"/> instances without regard
/// to casing. Using this comparer for that cache keeps lookups and additions consistent, so differently cased
/// spellings of the same event store name resolve to a single instance rather than accumulating as separate
/// entries. The key kept in the cache is the name as it was first registered, so the observable casing of an
/// event store name is unaffected.
/// <para>
/// Comparison is ordinal, matching how event store names are compared everywhere else in the kernel — they are
/// identifiers that become database names, not linguistic text. Kernel projects build with
/// <c>InvariantGlobalization</c> enabled, where culture-aware comparison already collapses to ordinal semantics,
/// so this only narrows resolution in a host that turns invariant globalization back off.
/// </para>
/// </remarks>
public sealed class CaseInsensitiveEventStoreNameComparer : EqualityComparer<EventStoreName>
{
    /// <summary>
    /// Gets the singleton instance of the comparer.
    /// </summary>
    public static readonly CaseInsensitiveEventStoreNameComparer Instance = new();

    /// <inheritdoc/>
    public override bool Equals(EventStoreName? x, EventStoreName? y) =>
        ReferenceEquals(x, y) ||
        (x is not null && y is not null && string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public override int GetHashCode(EventStoreName obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Value ?? string.Empty);
}
