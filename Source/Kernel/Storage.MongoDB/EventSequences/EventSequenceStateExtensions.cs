// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/>.
/// </summary>
public static class EventSequenceStateExtensions
{
    /// <summary>
    /// Convert to <see cref="EventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="Chronicle.Storage.EventSequences.EventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="EventSequenceState"/>.</returns>
    public static EventSequenceState ToMongoDB(this Chronicle.Storage.EventSequences.EventSequenceState state)
        => new(
            state.SequenceNumber,
            state.TailSequenceNumberPerEventType?.ToDictionary(_ => _.Key.Value, _ => _.Value) ?? []);

    /// <summary>
    /// Convert to <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="EventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="Chronicle.Storage.EventSequences.EventSequenceState"/>.</returns>
    public static Chronicle.Storage.EventSequences.EventSequenceState ToChronicle(this EventSequenceState state) =>
        new()
        {
            SequenceNumber = state.SequenceNumber,
            TailSequenceNumberPerEventType = state.TailSequenceNumberPerEventType?.ToDictionary(_ => (EventTypeId)_.Key, _ => _.Value) ?? []
        };
}
