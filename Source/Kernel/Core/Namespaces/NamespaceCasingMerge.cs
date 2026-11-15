// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Selects a single stored casing for each namespace without changing its data.
/// </summary>
internal static class NamespaceCasingMerge
{
    /// <summary>
    /// Merges case variants, preferring the name with its own data and then creation and storage order.
    /// </summary>
    /// <param name="stored">Namespaces in storage order.</param>
    /// <param name="withOwnData">Names with data under exactly their own casing.</param>
    /// <returns>The kept namespaces and variants to remove from the list.</returns>
    internal static (IList<NamespaceState> Kept, IList<(EventStoreNamespaceName Variant, EventStoreNamespaceName Canonical)> Merged) Merge(
        IReadOnlyList<NamespaceState> stored,
        IReadOnlySet<EventStoreNamespaceName> withOwnData)
    {
        var kept = new List<(int Index, NamespaceState State)>();
        var merged = new List<(EventStoreNamespaceName Variant, EventStoreNamespaceName Canonical)>();
        foreach (var group in stored.Select((state, index) => (State: state, Index: index)).GroupBy(_ => _.State.Name.Value, StringComparer.OrdinalIgnoreCase))
        {
            var members = group.ToArray();
            var withData = members.Where(_ => withOwnData.Contains(_.State.Name)).ToArray();
            var selected = (withData.Length == 1 ? withData : members).OrderBy(_ => _.State.Created).ThenBy(_ => _.Index).First();
            var canonical = group.Key.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase)
                ? EventStoreNamespaceName.Default
                : selected.State.Name;
            kept.Add((selected.Index, selected.State with { Name = canonical }));
            merged.AddRange(members.Where(_ => _.State.Name != canonical).Select(_ => (_.State.Name, canonical)));
        }

        return (kept.OrderBy(_ => _.Index).Select(_ => _.State).ToList(), merged);
    }
}
