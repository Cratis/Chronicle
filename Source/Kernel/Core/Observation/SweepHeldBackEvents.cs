// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The live events an observer held back while a catch-up sweep was in flight, in the order they were delivered, so
/// that the part the sweep did not reach can be handed straight to live delivery when the sweep reports back.
/// </summary>
/// <param name="capacity">The number of the most recent events to keep. Older batches are let go once it is exceeded.</param>
/// <remarks>
/// <para>
/// The appended events queue delivers an observer's events in sequence order, so once one event has been delivered
/// every later event either has been delivered too or is still on its way. Everything at or above the first event
/// held back is therefore here, apart from what was let go to stay within the capacity - and what was let go is
/// always older than what is kept. <see cref="Covers"/> uses that to tell whether the events from a position onwards
/// are all accounted for: either here, or still to be delivered live.
/// </para>
/// <para>
/// That only holds while the queue subscription that delivered them stays in place, so the observer discards the
/// whole instance when it leaves the state that holds the subscription.
/// </para>
/// </remarks>
public class SweepHeldBackEvents(int capacity)
{
    readonly Queue<(Key Partition, AppendedEvent[] Events)> _batches = new();
    int _count;
    EventSequenceNumber _first = EventSequenceNumber.Unavailable;
    EventSequenceNumber _letGoThrough = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets the number of events kept.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Keeps a batch of events delivered for a partition while live delivery was held back.
    /// </summary>
    /// <param name="partition">The partition the events were delivered for.</param>
    /// <param name="events">The events delivered.</param>
    public void Add(Key partition, IEnumerable<AppendedEvent> events)
    {
        var batch = events.ToArray();
        if (batch.Length == 0) return;

        var lowest = (EventSequenceNumber)batch.Min(_ => _.Context.SequenceNumber.Value);
        if (!_first.IsActualValue || lowest < _first)
        {
            _first = lowest;
        }

        _batches.Enqueue((partition, batch));
        _count += batch.Length;

        while (_count > capacity && _batches.Count > 1)
        {
            var (_, oldest) = _batches.Dequeue();
            _count -= oldest.Length;
            var highest = (EventSequenceNumber)oldest.Max(_ => _.Context.SequenceNumber.Value);
            if (!_letGoThrough.IsActualValue || highest > _letGoThrough)
            {
                _letGoThrough = highest;
            }
        }
    }

    /// <summary>
    /// Gets whether every event at or above a position is either kept here or not yet delivered.
    /// </summary>
    /// <param name="from">The position.</param>
    /// <returns>True if nothing at or above the position was delivered and then lost.</returns>
    public bool Covers(EventSequenceNumber from) =>
        from.IsActualValue &&
        _first.IsActualValue &&
        _first <= from &&
        (!_letGoThrough.IsActualValue || _letGoThrough < from);

    /// <summary>
    /// Gets the batches kept, in the order they were delivered, with only the events at or above a position.
    /// </summary>
    /// <param name="from">The position.</param>
    /// <returns>The partitions and their events, leaving out batches with nothing at or above the position.</returns>
    public IEnumerable<(Key Partition, AppendedEvent[] Events)> From(EventSequenceNumber from) =>
        _batches
            .Select(batch => (batch.Partition, Events: batch.Events.Where(_ => _.Context.SequenceNumber >= from).ToArray()))
            .Where(batch => batch.Events.Length > 0)
            .ToArray();
}
