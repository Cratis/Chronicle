// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Converter for <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/> to and from <see cref="EventSequenceState"/>.
/// </summary>
public static class EventSequenceStateConverter
{
    /// <summary>
    /// Convert from <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/> to <see cref="EventSequenceState"/>.
    /// </summary>
    /// <param name="state">The state to convert.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>The <see cref="EventSequenceState"/>.</returns>
    public static EventSequenceState ToEventSequenceStateEntry(Chronicle.Storage.EventSequences.EventSequenceState state, EventSequenceId eventSequenceId)
    {
        var tailSequenceNumbers = state.TailSequenceNumberPerEventType?.ToDictionary<KeyValuePair<EventTypeId, EventSequenceNumber>, string, object>(
            kvp => kvp.Key.Value,
            kvp => kvp.Value.Value) ?? new Dictionary<string, object>();

        return new EventSequenceState
        {
            EventSequenceId = eventSequenceId.Value,
            SequenceNumber = state.SequenceNumber.Value,
            TailSequenceNumberPerEventType = tailSequenceNumbers
        };
    }

    /// <summary>
    /// Convert from <see cref="EventSequenceState"/> to <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/>.
    /// </summary>
    /// <param name="entry">The entry to convert.</param>
    /// <returns>The <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/>.</returns>
    public static Chronicle.Storage.EventSequences.EventSequenceState ToEventSequenceState(EventSequenceState entry)
    {
        var tailSequenceNumbers = new Dictionary<EventTypeId, EventSequenceNumber>();

        if (entry.TailSequenceNumberPerEventType?.Count > 0)
        {
            tailSequenceNumbers = entry.TailSequenceNumberPerEventType.ToDictionary(
                kvp => new EventTypeId(kvp.Key),
                kvp => new EventSequenceNumber(Convert.ToUInt64(kvp.Value)));
        }

        return new Chronicle.Storage.EventSequences.EventSequenceState
        {
            SequenceNumber = new EventSequenceNumber(entry.SequenceNumber),
            TailSequenceNumberPerEventType = tailSequenceNumbers
        };
    }
}
