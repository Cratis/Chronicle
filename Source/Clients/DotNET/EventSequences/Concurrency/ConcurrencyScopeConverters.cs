// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Converter methods for <see cref="ConcurrencyScope"/>.
/// </summary>
internal static class ConcurrencyScopeConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="ConcurrencyScope"/>.
    /// </summary>
    /// <param name="scope"><see cref="ConcurrencyScope"/> to convert.</param>
    /// <returns>Converted contract version.</returns>
    /// <remarks>
    /// <see cref="EventSequenceNumber.BeforeFirst"/> is an in-process value and never goes on the wire. A scope
    /// expecting no matching event says so in its own field and sends <see cref="EventSequenceNumber.Unavailable"/>
    /// as the number, because that is the value a kernel too old to know the field already declines to validate.
    /// Sending the distinguished number instead would make such a kernel compare a real tail against a number
    /// nothing can exceed - a concurrency check that reports success without running, which is worse than the
    /// skip this whole change set out to remove.
    /// </remarks>
    internal static Contracts.EventSequences.Concurrency.ConcurrencyScope ToContract(this ConcurrencyScope scope) => new()
    {
        SequenceNumber = scope.ExpectsNoMatchingEvent ? EventSequenceNumber.Unavailable.Value : scope.SequenceNumber.Value,
        ExpectsNoMatchingEvent = scope.ExpectsNoMatchingEvent,
        EventSourceId = scope.EventSourceId is not null,
        EventStreamType = scope.EventStreamType?.Value,
        EventStreamId = scope.EventStreamId?.Value,
        EventSourceType = scope.EventSourceType?.Value,
        EventTypes = scope.EventTypes?.ToContract()
    };
}
