// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="Storage.EventSequences.EventSequenceState"/>.
/// </summary>
public static class EventSequenceStateExtensions
{
    /// <summary>
    /// Convert to <see cref="EventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="Storage.EventSequences.EventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="EventSequenceState"/>.</returns>
    public static EventSequenceState ToMongoDB(this Storage.EventSequences.EventSequenceState state)
        => new(
            state.SequenceNumber,
            state.TailSequenceNumberPerEventType?.ToDictionary(_ => _.Key.Value.ToString(), _ => _.Value) ?? new Dictionary<string, EventSequenceNumber>());

    /// <summary>
    /// Convert to <see cref="Storage.EventSequences.EventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="EventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="Storage.EventSequences.EventSequenceState"/>.</returns>
    public static Storage.EventSequences.EventSequenceState ToKernel(this EventSequenceState state) =>
        new()
        {
            SequenceNumber = state.SequenceNumber,
            TailSequenceNumberPerEventType = state.TailSequenceNumberPerEventType?.ToDictionary(_ => (EventTypeId)_.Key, _ => _.Value) ?? new Dictionary<EventTypeId, EventSequenceNumber>()
        };
}
