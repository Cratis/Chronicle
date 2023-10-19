// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.EventSequences;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Extension methods for working with <see cref="EventSequenceState"/>.
/// </summary>
public static class EventSequenceStateExtensions
{
    /// <summary>
    /// Convert to <see cref="MongoDBEventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="EventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="MongoDBEventSequenceState"/>.</returns>
    public static MongoDBEventSequenceState ToMongoDB(this EventSequenceState state)
        => new(
            state.SequenceNumber,
            state.TailSequenceNumberPerEventType?.ToDictionary(_ => _.Key.Value.ToString(), _ => _.Value) ?? new Dictionary<string, EventSequenceNumber>());

    /// <summary>
    /// Convert to <see cref="EventSequenceState"/>.
    /// </summary>
    /// <param name="state"><see cref="MongoDBEventSequenceState"/> to convert.</param>
    /// <returns>Converted <see cref="EventSequenceState"/>.</returns>
    public static EventSequenceState ToKernel(this MongoDBEventSequenceState state) =>
        new()
        {
            SequenceNumber = state.SequenceNumber,
            TailSequenceNumberPerEventType = state.TailSequenceNumberPerEventType?.ToDictionary(_ => (EventTypeId)_.Key, _ => _.Value) ?? new Dictionary<EventTypeId, EventSequenceNumber>()
        };
}
