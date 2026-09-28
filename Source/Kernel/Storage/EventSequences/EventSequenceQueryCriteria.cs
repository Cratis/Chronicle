// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the criteria that narrow an event sequence query made for presentation purposes.
/// </summary>
/// <param name="EventSourceId">Optional <see cref="EventSourceId"/> to narrow to.</param>
/// <param name="EventSourceType">Optional <see cref="EventSourceType"/> to narrow to.</param>
/// <param name="EventStreamType">Optional <see cref="EventStreamType"/> to narrow to.</param>
/// <param name="CorrelationId">Optional <see cref="CorrelationId"/> to narrow to.</param>
/// <param name="EventTypes">Optional <see cref="EventType">event types</see> to narrow to - an event matches when it is any of them.</param>
/// <param name="Tags">Optional <see cref="Tag">tags</see> to narrow to - an event matches when it carries any of them.</param>
/// <param name="OccurredFrom">Optional inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">Optional exclusive upper bound on when the event occurred.</param>
/// <remarks>
/// Every member is optional and a <see langword="null"/> or empty value means "do not narrow on this
/// dimension". Callers asking for everything pass <see cref="Empty"/>, never a set of sentinels, so an
/// implementation must treat an absent value as "match all" rather than as a value to compare against.
/// The sentinel each dimension carries for "everything" - an unspecified <see cref="EventSourceId"/>,
/// an unspecified <see cref="EventSourceType"/>, <see cref="EventStreamType.All"/>, a
/// <see cref="CorrelationId.NotSet"/>, an empty event type or tag set - is treated the same way, so a
/// caller that passes one of those rather than <see langword="null"/> still gets every event back.
/// An absent or empty <see cref="NamedTags"/> collection does not narrow; within a named criterion,
/// absent values match any value for that name, but an explicitly empty value set is invalid.
/// </remarks>
public record EventSequenceQueryCriteria(
    EventSourceId? EventSourceId = null,
    EventSourceType? EventSourceType = null,
    EventStreamType? EventStreamType = null,
    CorrelationId? CorrelationId = null,
    IEnumerable<EventType>? EventTypes = null,
    IEnumerable<Tag>? Tags = null,
    DateTimeOffset? OccurredFrom = null,
    DateTimeOffset? OccurredTo = null)
{
    /// <summary>
    /// Gets the criteria that narrows nothing - every event in the sequence matches.
    /// </summary>
    public static readonly EventSequenceQueryCriteria Empty = new();

    /// <summary>
    /// Gets the optional named tag criteria. Any one of them may match, and the dimension combines with the other dimensions.
    /// </summary>
    /// <exception cref="InvalidNamedTagCriterion">The collection contains a null criterion.</exception>
    public IEnumerable<NamedTagCriterion>? NamedTags
    {
        get;
        init
        {
            if (value is null)
            {
                field = null;
                return;
            }

            var snapshot = value.ToArray();
            if (snapshot.Any(criterion => criterion is null))
            {
                throw new InvalidNamedTagCriterion();
            }

            field = Array.AsReadOnly(snapshot);
        }
    }

    /// <summary>
    /// Gets a value indicating whether named tags narrow the query.
    /// </summary>
    public bool HasNamedTags => NamedTags?.Any() == true;

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on the event source.
    /// </summary>
    public bool HasEventSourceId => EventSourceId?.IsSpecified == true;

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on the event source type.
    /// </summary>
    public bool HasEventSourceType => EventSourceType is { Value.Length: > 0 };

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on the event stream type.
    /// </summary>
    public bool HasEventStreamType => EventStreamType is { Value.Length: > 0, IsAll: false };

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on the correlation.
    /// </summary>
    public bool HasCorrelationId => CorrelationId is not null && CorrelationId != Execution.CorrelationId.NotSet;

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on event types.
    /// </summary>
    public bool HasEventTypes => EventTypes?.Any() == true;

    /// <summary>
    /// Gets a value indicating whether the criteria narrows on tags.
    /// </summary>
    public bool HasTags => Tags?.Any() == true;

    /// <summary>
    /// Determine whether an event matches the criteria.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event.</param>
    /// <returns>True if the event matches every dimension the criteria narrows on, false otherwise.</returns>
    public bool Matches(EventContext context)
    {
        if (HasEventSourceId && EventSourceId != context.EventSourceId)
        {
            return false;
        }

        if (HasEventSourceType && EventSourceType != context.EventSourceType)
        {
            return false;
        }

        if (HasEventStreamType && EventStreamType != context.EventStreamType)
        {
            return false;
        }

        if (HasCorrelationId && CorrelationId != context.CorrelationId)
        {
            return false;
        }

        if (HasEventTypes && !EventTypes!.Any(_ => _.Id == context.EventType.Id))
        {
            return false;
        }

        if (HasTags && !Tags!.Any(tag => context.Tags.Any(_ => _ == tag)))
        {
            return false;
        }

        if (HasNamedTags && !NamedTags!.Any(criterion => context.NamedTags.Any(tag =>
            string.Equals(tag.Name.Value, criterion.Name.Value, StringComparison.Ordinal) &&
            criterion.Values?.Contains(tag.Value, StringComparer.Ordinal) != false)))
        {
            return false;
        }

        if (OccurredFrom is not null && context.Occurred < OccurredFrom)
        {
            return false;
        }

        return OccurredTo is null || context.Occurred < OccurredTo;
    }
}
