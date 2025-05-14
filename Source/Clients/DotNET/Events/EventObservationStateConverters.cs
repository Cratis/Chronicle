// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventObservationState"/>.
/// </summary>
internal static class EventObservationStateConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="EventObservationState"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    internal static Contracts.Events.EventObservationState ToContract(this EventObservationState state) => state switch
    {
        EventObservationState.None => Contracts.Events.EventObservationState.None,
        EventObservationState.Initial => Contracts.Events.EventObservationState.Initial,
        EventObservationState.Replay => Contracts.Events.EventObservationState.Replay,
        _ => Contracts.Events.EventObservationState.None
    };

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="Contracts.Events.EventObservationState"/> to convert.</param>
    /// <returns>Converted <see cref="EventObservationState"/>.</returns>
    internal static EventObservationState ToClient(this Contracts.Events.EventObservationState state) => state switch
    {
        Contracts.Events.EventObservationState.None => EventObservationState.None,
        Contracts.Events.EventObservationState.Initial => EventObservationState.Initial,
        Contracts.Events.EventObservationState.Replay => EventObservationState.Replay,
        _ => EventObservationState.None
    };
}
