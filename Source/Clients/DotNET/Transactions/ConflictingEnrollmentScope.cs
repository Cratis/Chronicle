// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an enrollment cannot carry its required concurrency scope.
/// The message is redacted by design; values are available through <see cref="EventSourceId"/>,
/// <see cref="Effective"/>, <see cref="Required"/>, and <see cref="EventSequenceId"/>.
/// </summary>
/// <param name="eventSourceId">The event source being enrolled.</param>
/// <param name="effective">The effective scope.</param>
/// <param name="required">The required scope that conflicts with the enrollment.</param>
/// <param name="eventSequenceId">The attempted event sequence, if known.</param>
public class ConflictingEnrollmentScope(EventSourceId eventSourceId, ConcurrencyScope effective, ConcurrencyScope required, EventSequenceId? eventSequenceId)
    : Exception(CreateMessage(effective, required))
{
    /// <summary>
    /// Initializes a conflict without event-sequence information.
    /// </summary>
    /// <param name="eventSourceId">The event source being enrolled.</param>
    /// <param name="effective">The effective scope.</param>
    /// <param name="required">The required scope that conflicts with the enrollment.</param>
    public ConflictingEnrollmentScope(EventSourceId eventSourceId, ConcurrencyScope effective, ConcurrencyScope required)
        : this(eventSourceId, effective, required, null)
    {
    }

    /// <summary>
    /// Gets the event source being enrolled.
    /// </summary>
    public EventSourceId EventSourceId { get; } = eventSourceId;

    /// <summary>
    /// Gets the supplied or effective scope that conflicts with the requirement.
    /// </summary>
    public ConcurrencyScope Effective { get; } = effective;

    /// <summary>
    /// Gets the required scope that the enrollment cannot carry.
    /// </summary>
    public ConcurrencyScope Required { get; } = required;

    /// <summary>
    /// Gets the attempted event sequence, or null if it was not supplied.
    /// </summary>
    public EventSequenceId? EventSequenceId { get; } = eventSequenceId;

    static string CreateMessage(ConcurrencyScope effective, ConcurrencyScope required)
    {
        (string Name, bool Differs)[] dimensions =
        [
            ("sequence number", effective.SequenceNumber != required.SequenceNumber),
            ("event source id", effective.EventSourceId?.Value != required.EventSourceId?.Value),
            ("event source type", Normalize(effective.EventSourceType) != Normalize(required.EventSourceType)),
            ("event stream type", Normalize(effective.EventStreamType) != Normalize(required.EventStreamType)),
            ("event stream id", Normalize(effective.EventStreamId) != Normalize(required.EventStreamId)),
            ("event types", !(effective.EventTypes?.ToHashSet() ?? []).SetEquals(required.EventTypes ?? []))
        ];
        var differingDimensions = dimensions.Where(_ => _.Differs).Select(_ => _.Name).ToArray();
        var details = differingDimensions.Length == 0 ? string.Empty : $" Differing dimensions: {string.Join(", ", differingDimensions)}.";
        return $"Enrollment conflicts with its required scope or event sequence.{details}";
    }

    static EventStreamType? Normalize(EventStreamType? value) => value == EventStreamType.All ? null : value;
    static EventStreamId? Normalize(EventStreamId? value) => value == EventStreamId.Default ? null : value;
    static EventSourceType? Normalize(EventSourceType? value) => value == EventSourceType.Default || value == EventSourceType.Unspecified ? null : value;
}
