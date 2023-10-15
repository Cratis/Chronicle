// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Events;

/// <summary>
/// Converter methods for <see cref="EventContext"/>.
/// </summary>
public static class EventContextConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="Kernel.Contracts.Events.EventContext"/>.</returns>
    public static Kernel.Contracts.Events.EventContext ToContract(this EventContext context) => new()
    {
        EventSourceId = context.EventSourceId,
        SequenceNumber = context.SequenceNumber,
        Occurred = context.Occurred!,
        ValidFrom = context.ValidFrom!,
        TenantId = context.TenantId,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.Select(_ => _.ToContract()),
        CausedBy = context.CausedBy.ToContract(),
        ObservationState = context.ObservationState.ToContract()
    };

    /// <summary>
    /// Convert to kernel version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="Kernel.Contracts.Events.EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="EventContext"/>.</returns>
    public static EventContext ToKernel(this Kernel.Contracts.Events.EventContext context) => new(
        context.EventSourceId,
        context.SequenceNumber,
        context.Occurred,
        context.ValidFrom,
        context.TenantId,
        context.CorrelationId,
        context.Causation.Select(_ => _.ToKernel()),
        context.CausedBy.ToKernel(),
        context.ObservationState.ToKernel());
}
