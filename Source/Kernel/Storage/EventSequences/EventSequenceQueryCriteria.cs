// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents the criteria that narrow an event sequence query made for presentation purposes.
/// </summary>
/// <param name="EventSourceId">Optional <see cref="EventSourceId"/> to narrow to.</param>
/// <param name="EventTypes">Optional <see cref="EventType">event types</see> to narrow to - an event matches when it is any of them.</param>
/// <param name="Tags">Optional <see cref="Tag">tags</see> to narrow to - an event matches when it carries any of them.</param>
/// <param name="OccurredFrom">Optional inclusive lower bound on when the event occurred.</param>
/// <param name="OccurredTo">Optional exclusive upper bound on when the event occurred.</param>
/// <remarks>
/// Every member is optional and a <see langword="null"/> or empty value means "do not narrow on this
/// dimension". Callers asking for everything pass <see cref="Empty"/>, never a set of sentinels, so an
/// implementation must treat an absent value as "match all" rather than as a value to compare against.
/// </remarks>
public record EventSequenceQueryCriteria(
    EventSourceId? EventSourceId = null,
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
    /// Gets a value indicating whether the criteria narrows on the event source.
    /// </summary>
    public bool HasEventSourceId => EventSourceId?.IsSpecified == true;

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
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event.</param>
    /// <param name="eventType">The <see cref="EventTypeId"/> of the event.</param>
    /// <param name="tags">The tags carried by the event.</param>
    /// <param name="occurred">When the event occurred.</param>
    /// <returns>True if the event matches every dimension the criteria narrows on, false otherwise.</returns>
    public bool Matches(EventSourceId eventSourceId, EventTypeId eventType, IEnumerable<string> tags, DateTimeOffset occurred)
    {
        if (HasEventSourceId && EventSourceId != eventSourceId)
        {
            return false;
        }

        if (HasEventTypes && !EventTypes!.Any(_ => _.Id == eventType))
        {
            return false;
        }

        if (HasTags && !Tags!.Any(tag => tags.Contains(tag.Value)))
        {
            return false;
        }

        if (OccurredFrom is not null && occurred < OccurredFrom)
        {
            return false;
        }

        return OccurredTo is null || occurred < OccurredTo;
    }
}
