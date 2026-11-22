// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// An immutable, ordered list of <see cref="EventType"/> that is shared by everything that refers to the same list.
/// </summary>
/// <remarks>
/// <para>
/// A store has a handful of distinct event type lists but thousands of observers, subscriptions and queue entries that
/// refer to them. Each of those used to hold a list of its own, and every grain call copied it. A set is created
/// through <see cref="From(IEnumerable{EventType})"/> (or a collection expression), which returns the instance already
/// alive in the silo for an equal list; deserialization does the same, so a list that crosses a grain boundary is
/// shared again on the other side. Instances are held weakly: a set lives for as long as something refers to it, and
/// the table that makes them shared does not keep one alive.
/// </para>
/// <para>
/// The set also carries the <see cref="Ids"/> as a precomputed <see cref="FrozenSet{T}"/>, which is what routing and
/// filtering look event types up in, so nobody builds one per subscription.
/// </para>
/// <para>
/// Two sets are equal when they hold the same event types in the same order, whether or not they are the same instance.
/// </para>
/// </remarks>
[CollectionBuilder(typeof(EventTypeSet), nameof(Create))]
[JsonConverter(typeof(EventTypeSetJsonConverter))]
public sealed class EventTypeSet : IReadOnlyList<EventType>, IEquatable<EventTypeSet>
{
    const int PruneThreshold = 4096;

    static readonly ConcurrentDictionary<string, WeakReference<EventTypeSet>> _shared = new();
    static int _pruneAt = PruneThreshold;

    readonly EventType[] _types;
    readonly int _hashCode;

    EventTypeSet(EventType[] types, string key)
    {
        _types = types;
        Key = key;
        _hashCode = key.GetHashCode(StringComparison.Ordinal);
        Ids = types.Select(_ => _.Id).ToFrozenSet();
    }

    /// <summary>
    /// Gets the empty set.
    /// </summary>
    public static EventTypeSet Empty { get; } = new([], string.Empty);

    /// <summary>
    /// Gets the number of distinct sets currently alive in the process. Counts sets that have been collected but not yet pruned.
    /// </summary>
    public static int Tracked => _shared.Count;

    /// <summary>
    /// Gets the <see cref="EventTypeId"/> of every event type in the set.
    /// </summary>
    public FrozenSet<EventTypeId> Ids { get; }

    /// <summary>
    /// Gets a value identifying the content of the set. Equal sets have equal keys.
    /// </summary>
    public string Key { get; }

    /// <inheritdoc/>
    public int Count => _types.Length;

    /// <inheritdoc/>
    public EventType this[int index] => _types[index];

    /// <summary>
    /// Implicitly converts an array of event types to a set.
    /// </summary>
    /// <param name="eventTypes">The event types.</param>
    public static implicit operator EventTypeSet(EventType[] eventTypes) => From(eventTypes);

    /// <summary>
    /// Gets the shared set for an ordered list of event types.
    /// </summary>
    /// <param name="eventTypes">The event types.</param>
    /// <returns>The <see cref="EventTypeSet"/>, which is the instance already alive for an equal list when there is one.</returns>
    public static EventTypeSet From(IEnumerable<EventType>? eventTypes)
    {
        if (eventTypes is EventTypeSet set)
        {
            return set;
        }

        var types = eventTypes?.Select(EventTypeInterner.Intern).ToArray() ?? [];
        return types.Length == 0 ? Empty : Share(types);
    }

    /// <summary>
    /// Collection builder for collection expressions.
    /// </summary>
    /// <param name="eventTypes">The event types.</param>
    /// <returns>The <see cref="EventTypeSet"/>.</returns>
    public static EventTypeSet Create(ReadOnlySpan<EventType> eventTypes) => From(eventTypes.ToArray());

    /// <summary>
    /// Gets whether the set holds an event type with the identifier.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to look for.</param>
    /// <returns>True if it does, false otherwise.</returns>
    public bool Contains(EventTypeId eventTypeId) => Ids.Contains(eventTypeId);

    /// <inheritdoc/>
    public IEnumerator<EventType> GetEnumerator() => ((IEnumerable<EventType>)_types).GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(EventTypeSet? other) => other is not null && (ReferenceEquals(this, other) || Key == other.Key);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as EventTypeSet);

    /// <inheritdoc/>
    public override int GetHashCode() => _hashCode;

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    static EventTypeSet Share(EventType[] types)
    {
        var key = string.Join('|', types.Select(_ => $"{_.Id}.{_.Generation}.{_.Tombstone}"));
        while (true)
        {
            if (_shared.TryGetValue(key, out var existing) && existing.TryGetTarget(out var alive))
            {
                return alive;
            }

            var created = new EventTypeSet(types, key);
            var reference = new WeakReference<EventTypeSet>(created);
            if (existing is null ? _shared.TryAdd(key, reference) : _shared.TryUpdate(key, reference, existing))
            {
                Prune();
                return created;
            }
        }
    }

    static void Prune()
    {
        if (_shared.Count < Volatile.Read(ref _pruneAt))
        {
            return;
        }

        foreach (var entry in _shared)
        {
            if (!entry.Value.TryGetTarget(out _))
            {
                _shared.TryRemove(entry);
            }
        }

        Volatile.Write(ref _pruneAt, Math.Max(PruneThreshold, _shared.Count * 2));
    }
}
