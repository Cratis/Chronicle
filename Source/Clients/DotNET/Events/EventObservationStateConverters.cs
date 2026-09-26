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
    /// <remarks>
    /// Mapped flag by flag rather than by whole value: this is a [Flags] enum, so a switch on the exact value
    /// silently answers None for any combination - which is how CatchUp, which travels alongside Initial,
    /// would have been erased on arrival.
    /// </remarks>
    internal static Contracts.Events.EventObservationState ToContract(this EventObservationState state)
    {
        var result = Contracts.Events.EventObservationState.None;
        if (state.HasFlag(EventObservationState.Initial)) result |= Contracts.Events.EventObservationState.Initial;
        if (state.HasFlag(EventObservationState.Replay)) result |= Contracts.Events.EventObservationState.Replay;
        if (state.HasFlag(EventObservationState.CatchUp)) result |= Contracts.Events.EventObservationState.CatchUp;
        return result;
    }

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventObservationState"/>.
    /// </summary>
    /// <param name="state"><see cref="Contracts.Events.EventObservationState"/> to convert.</param>
    /// <returns>Converted <see cref="EventObservationState"/>.</returns>
    /// <remarks>
    /// Mapped flag by flag rather than by whole value: this is a [Flags] enum, so a switch on the exact value
    /// silently answers None for any combination - which is how CatchUp, which travels alongside Initial,
    /// would have been erased on arrival.
    /// </remarks>
    internal static EventObservationState ToClient(this Contracts.Events.EventObservationState state)
    {
        var result = EventObservationState.None;
        if (state.HasFlag(Contracts.Events.EventObservationState.Initial)) result |= EventObservationState.Initial;
        if (state.HasFlag(Contracts.Events.EventObservationState.Replay)) result |= EventObservationState.Replay;
        if (state.HasFlag(Contracts.Events.EventObservationState.CatchUp)) result |= EventObservationState.CatchUp;
        return result;
    }
}
