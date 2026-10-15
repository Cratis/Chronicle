// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Describes a resolved condition validated atomically with an append.
/// </summary>
/// <param name="Label">The condition label.</param>
/// <param name="EventSourceId">Optional source narrowing.</param>
/// <param name="EventTypes">Type-ID narrowing; empty means any type.</param>
/// <param name="EventSourceType">Optional source type.</param>
/// <param name="EventStreamType">Optional stream type.</param>
/// <param name="EventStreamId">Optional stream identifier.</param>
/// <param name="After">The retained boundary.</param>
/// <param name="ExpectsNoMatchingEvent">Whether no matching event may exist.</param>
/// <param name="Origins">The dependencies supplying this condition.</param>
/// <param name="Reads">Descriptions of protected reads.</param>
public record AppendCondition(
    string Label,
    EventSourceId? EventSourceId,
    IReadOnlyList<EventType> EventTypes,
    EventSourceType? EventSourceType,
    EventStreamType? EventStreamType,
    EventStreamId? EventStreamId,
    EventSequenceNumber After,
    bool ExpectsNoMatchingEvent,
    IReadOnlyList<AppendConditionOrigin> Origins,
    IReadOnlyList<string> Reads)
{
    /// <inheritdoc/>
    public override string ToString()
    {
        var types = EventTypes.Count == 0 ? "any type" : $"types=[{string.Join(',', EventTypes.Select(_ => _.Id.Value))}]";
        var dimensions = new[]
        {
            EventSourceType is null ? null : $"sourceType={EventSourceType}",
            EventStreamType is null ? null : $"streamType={EventStreamType}",
            EventStreamId is null ? null : $"streamId={EventStreamId}"
        }.Where(_ => _ is not null);
        var boundary = ExpectsNoMatchingEvent ? "no matching event" : $"after {After}";
        return $"guard {Label}: {string.Join(", ", new[] { types }.Concat(dimensions))}, {boundary}";
    }
}
