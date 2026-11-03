// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the implementation of <see cref="IResourceNameSnapshot"/> publishing an immutable state with a single volatile write.
/// </summary>
public sealed class ResourceNameSnapshot : IResourceNameSnapshot
{
    static readonly State _empty = new(true, new HashSet<string>(), new HashSet<string>());

    volatile State _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNameSnapshot"/> class, ready and with nothing quarantined.
    /// </summary>
    public ResourceNameSnapshot() => _state = _empty;

    /// <inheritdoc/>
    public bool IsReady => _state.IsReady;

    /// <inheritdoc/>
    public bool HasQuarantine => _state.HasQuarantine;

    /// <summary>
    /// Create a snapshot that is not ready until <see cref="Publish"/> has been called.
    /// </summary>
    /// <returns>A new <see cref="ResourceNameSnapshot"/>.</returns>
    public static ResourceNameSnapshot NotReady() => new() { _state = new State(false, new HashSet<string>(), new HashSet<string>()) };

    /// <summary>
    /// Publish a new state, which makes the snapshot ready.
    /// </summary>
    /// <param name="eventStores">The quarantined event stores.</param>
    /// <param name="namespaces">The quarantined namespaces, by event store.</param>
    public void Publish(IEnumerable<EventStoreName> eventStores, IEnumerable<(EventStoreName EventStore, EventStoreNamespaceName Namespace)> namespaces) =>
        _state = new State(
            true,
            eventStores.Select(_ => _.Value).ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            namespaces.Select(_ => Key(_.EventStore.Value, _.Namespace.Value)).ToFrozenSet(StringComparer.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public ResourceNameStatus Check(string eventStore, string? @namespace = null)
    {
        var state = _state;
        if (state.IsReady && !state.HasQuarantine)
        {
            return ResourceNameStatus.Available;
        }

        if (!state.IsReady)
        {
            return eventStore == EventStoreName.System.Value ? ResourceNameStatus.Available : ResourceNameStatus.NotReady;
        }

        return state.EventStores.Contains(eventStore) || (@namespace is not null && state.Namespaces.Contains(Key(eventStore, @namespace)))
            ? ResourceNameStatus.Quarantined
            : ResourceNameStatus.Available;
    }

    /// <inheritdoc/>
    public bool IsQuarantined(EventStoreName eventStore) => Check(eventStore.Value) == ResourceNameStatus.Quarantined;

    /// <inheritdoc/>
    public bool IsQuarantined(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Check(eventStore.Value, @namespace.Value) == ResourceNameStatus.Quarantined;

    static string Key(string eventStore, string @namespace) => $"{eventStore}#{@namespace}";

    sealed record State(bool IsReady, IReadOnlySet<string> EventStores, IReadOnlySet<string> Namespaces)
    {
        public bool HasQuarantine => EventStores.Count > 0 || Namespaces.Count > 0;
    }
}
