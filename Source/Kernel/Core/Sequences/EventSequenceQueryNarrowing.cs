// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the narrowing a workbench query asks for, as it arrives over HTTP.
/// </summary>
/// <param name="EventSourceId">Optional event source to narrow to.</param>
/// <param name="EventSourceType">Optional event source type to narrow to.</param>
/// <param name="EventStreamType">Optional event stream type to narrow to.</param>
/// <param name="CorrelationId">Optional correlation identifier to narrow to.</param>
/// <param name="EventTypeIds">Optional comma separated event type identifiers to narrow to.</param>
/// <param name="Tags">Optional comma separated tags to narrow to.</param>
/// <param name="OccurredFrom">Optional inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">Optional exclusive upper bound on when the event occurred.</param>
/// <remarks>
/// Every value is the raw string the caller sent, so that turning "nothing was supplied" into "do not
/// narrow on this dimension" happens in one place - <see cref="EventSequenceQueryCriteriaFactory"/> -
/// rather than at each endpoint.
/// </remarks>
public record EventSequenceQueryNarrowing(
    string? EventSourceId = null,
    string? EventSourceType = null,
    string? EventStreamType = null,
    string? CorrelationId = null,
    string? EventTypeIds = null,
    string? Tags = null,
    DateTimeOffset? OccurredFrom = null,
    DateTimeOffset? OccurredTo = null)
{
    /// <summary>
    /// Gets the narrowing that narrows nothing.
    /// </summary>
    public static readonly EventSequenceQueryNarrowing None = new();
}
