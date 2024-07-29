// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventContext"/>.
/// </summary>
public static class EventContextConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Events.EventContext"/>.</returns>
    public static Contracts.Events.EventContext ToContract(this EventContext context) => new()
    {
        EventSourceId = context.EventSourceId,
        SequenceNumber = context.SequenceNumber,
        Occurred = context.Occurred!,
        EventStore = context.EventStore,
        Namespace = context.Namespace,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.Select(_ => _.ToContract()).ToList(),
        CausedBy = context.CausedBy.ToContract(),
        ObservationState = context.ObservationState.ToContract()
    };

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="Contracts.Events.EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="EventContext"/>.</returns>
    public static EventContext ToClient(this Contracts.Events.EventContext context) => new(
        context.EventSourceId,
        context.SequenceNumber,
        context.Occurred,
        context.EventStore,
        context.Namespace,
        context.CorrelationId,
        context.Causation.Select(_ => _.ToClient()),
        context.CausedBy.ToClient(),
        context.ObservationState.ToClient());
}
