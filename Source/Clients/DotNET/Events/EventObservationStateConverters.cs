// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventObservationState"/>.
/// </summary>
public static class EventObservationStateConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="EventObservationState"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    public static Chronicle.Contracts.Events.EventObservationState ToContract(this EventObservationState state) => state switch
    {
        EventObservationState.None => Chronicle.Contracts.Events.EventObservationState.None,
        EventObservationState.Initial => Chronicle.Contracts.Events.EventObservationState.Initial,
        EventObservationState.Replay => Chronicle.Contracts.Events.EventObservationState.Replay,
        _ => Chronicle.Contracts.Events.EventObservationState.None
    };

    /// <summary>
    /// Convert to kernel version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="Chronicle.Contracts.Events.EventObservationState"/> to convert.</param>
    /// <returns>Converted <see cref="EventObservationState"/>.</returns>
    public static EventObservationState ToClient(this Chronicle.Contracts.Events.EventObservationState state) => state switch
    {
        Chronicle.Contracts.Events.EventObservationState.None => EventObservationState.None,
        Chronicle.Contracts.Events.EventObservationState.Initial => EventObservationState.Initial,
        Chronicle.Contracts.Events.EventObservationState.Replay => EventObservationState.Replay,
        _ => EventObservationState.None
    };
}
