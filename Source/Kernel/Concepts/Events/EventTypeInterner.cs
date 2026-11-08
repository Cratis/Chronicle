// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Shares one instance of each distinct <see cref="EventType"/>, and of each distinct list of them.
/// </summary>
/// <remarks>
/// Every job step carries the event types its observer reads, as an identical list of a few dozen types, and each step is
/// deserialized on its own. A job over thousands of partitions then holds thousands of copies of the same few dozen types,
/// each with its identifier string and generation. Event types are immutable, so sharing them is safe.
/// </remarks>
public static class EventTypeInterner
{
    const int MaximumDistinct = 4096;

    static readonly ConcurrentDictionary<EventType, EventType> _types = new();
    static readonly ConcurrentDictionary<string, EventType[]> _lists = new();

    /// <summary>
    /// Gets the shared instance equal to an <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType">The <see cref="EventType"/>.</param>
    /// <returns>The shared <see cref="EventType"/>.</returns>
    public static EventType Intern(EventType eventType)
    {
        if (_types.Count >= MaximumDistinct)
        {
            _types.Clear();
        }

        return _types.GetOrAdd(eventType, eventType);
    }

    /// <summary>
    /// Gets the shared array holding the same event types in the same order.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventType">event types</see>.</param>
    /// <returns>The shared array. It must not be modified.</returns>
    public static EventType[] Intern(IEnumerable<EventType> eventTypes)
    {
        var shared = eventTypes.Select(Intern).ToArray();
        if (shared.Length == 0)
        {
            return shared;
        }

        if (_lists.Count >= MaximumDistinct)
        {
            _lists.Clear();
        }

        var key = string.Join('|', shared.Select(_ => $"{_.Id}.{_.Generation}.{_.Tombstone}"));
        return _lists.GetOrAdd(key, shared);
    }

    /// <summary>
    /// Gets a reader scope whose event types are shared.
    /// </summary>
    /// <param name="scope">The <see cref="ObserverReaderScope"/>, if any.</param>
    /// <returns>The scope with shared event types, or <see langword="null"/> when there was none.</returns>
    public static ObserverReaderScope? Intern(ObserverReaderScope? scope) =>
        scope is null || scope.EventTypes.Count == 0 ? scope : scope with { EventTypes = Intern(scope.EventTypes) };
}
