// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

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
    public static Kernel.Contracts.Events.EventObservationState ToContract(this EventObservationState state) => state switch
    {
        EventObservationState.None => Kernel.Contracts.Events.EventObservationState.None,
        EventObservationState.Initial => Kernel.Contracts.Events.EventObservationState.Initial,
        EventObservationState.Replay => Kernel.Contracts.Events.EventObservationState.Replay,
        _ => Kernel.Contracts.Events.EventObservationState.None
    };

    /// <summary>
    /// Convert to kernel version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="Kernel.Contracts.Events.EventObservationState"/> to convert.</param>
    /// <returns>Converted <see cref="EventObservationState"/>.</returns>
    public static EventObservationState ToKernel(this Kernel.Contracts.Events.EventObservationState state) => state switch
    {
        Kernel.Contracts.Events.EventObservationState.None => EventObservationState.None,
        Kernel.Contracts.Events.EventObservationState.Initial => EventObservationState.Initial,
        Kernel.Contracts.Events.EventObservationState.Replay => EventObservationState.Replay,
        _ => EventObservationState.None
    };
}
