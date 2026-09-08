// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Converts <see cref="EventContext"/> to its contract representation for the <see cref="CapturedEvent"/> read model.
/// </summary>
/// <remarks>
/// Captures is a read model, so its query methods put <see cref="Contracts.Events.EventContext"/> directly on the
/// record - the same shared-type situation as every other area Core still reaches into Contracts for directly.
/// The full conversion used to be reachable through the (now removed from Core) gRPC service layer; it is
/// duplicated here rather than depended on, following the same pattern <c>Cratis.Chronicle.EventTypes.EventTypeConverters</c>
/// and <c>Cratis.Chronicle.Identities.IdentityConverters</c> already use for their own areas.
/// </remarks>
internal static class EventContextConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Events.EventContext"/>.</returns>
    public static Contracts.Events.EventContext ToContract(this EventContext context) => new()
    {
        EventType = context.EventType.ToContract(),
        EventSourceType = context.EventSourceType,
        EventSourceId = context.EventSourceId,
        EventStreamType = context.EventStreamType,
        EventStreamId = context.EventStreamId,
        SequenceNumber = context.SequenceNumber,
        Occurred = context.Occurred,
        EventStore = context.EventStore,
        Namespace = context.Namespace,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.Select(ToContract).ToList(),
        CausedBy = context.CausedBy.ToContract(),
        Tags = context.Tags.Select(_ => _.Value),
        Hash = context.Hash,
        ObservationState = context.ObservationState.ToContract()
    };

    static Contracts.Auditing.Causation ToContract(this Concepts.Auditing.Causation causation) => new()
    {
        Occurred = causation.Occurred,
        Type = causation.Type,
        Properties = causation.Properties
    };

    static Contracts.Events.EventObservationState ToContract(this EventObservationState state) => state switch
    {
        EventObservationState.Initial => Contracts.Events.EventObservationState.Initial,
        EventObservationState.Replay => Contracts.Events.EventObservationState.Replay,
        _ => Contracts.Events.EventObservationState.None
    };
}
