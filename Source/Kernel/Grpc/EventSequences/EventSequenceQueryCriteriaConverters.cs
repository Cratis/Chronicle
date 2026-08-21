// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Services.EventSequences;

/// <summary>
/// Converters for <see cref="EventSequenceQueryCriteria"/>.
/// </summary>
public static class EventSequenceQueryCriteriaConverters
{
    /// <summary>
    /// Convert query criteria from its contract representation.
    /// </summary>
    /// <param name="criteria">The <see cref="Contracts.EventSequences.EventSequenceQueryCriteria"/> to convert.</param>
    /// <returns>The converted <see cref="EventSequenceQueryCriteria"/>.</returns>
    /// <remarks>
    /// Absent members stay absent rather than becoming a sentinel to compare against - the storage
    /// providers read "no value" as "do not narrow on this dimension".
    /// </remarks>
    public static EventSequenceQueryCriteria ToChronicle(this Contracts.EventSequences.EventSequenceQueryCriteria criteria) =>
        new(
            string.IsNullOrWhiteSpace(criteria.EventSourceId) ? null : new EventSourceId(criteria.EventSourceId),
            string.IsNullOrWhiteSpace(criteria.EventSourceType) ? null : new EventSourceType(criteria.EventSourceType),
            string.IsNullOrWhiteSpace(criteria.EventStreamType) ? null : new EventStreamType(criteria.EventStreamType),
            criteria.CorrelationId is null ? null : new CorrelationId(criteria.CorrelationId.Value),
            criteria.EventTypes.Count == 0 ? null : [.. criteria.EventTypes.ToChronicle()],
            criteria.Tags.Count == 0 ? null : [.. criteria.Tags.Select(tag => new Tag(tag))],
            criteria.OccurredFrom,
            criteria.OccurredTo);
}
