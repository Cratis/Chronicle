// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an event and the <see cref="EventSourceId"/> it is for.
/// </summary>
/// <param name="EventSourceId"><see cref="EventSourceId"/> the event is for.</param>
/// <param name="Event">The actual event.</param>
/// <param name="Causation">Optional causation for the event. If not set, the current causation chain is used.</param>
public record EventForEventSourceId(EventSourceId EventSourceId, object Event, Causation? Causation = default)
{
    /// <summary>
    /// Gets or inits the optional <see cref="Subject"/> for the event. When omitted, Chronicle derives it from
    /// a <see cref="SubjectAttribute"/> on the event, or falls back to <see cref="EventSourceId"/>.
    /// </summary>
    public Subject? Subject { get; init; }

    /// <summary>
    /// Gets or inits the <see cref="EventStreamType"/> for the event. The getter defaults to <see cref="EventStreamType.All"/>;
    /// leaving the property unset lets the kernel resolve its configured append default.
    /// </summary>
    public EventStreamType EventStreamType { get => RequestedEventStreamType ?? EventStreamType.All; init => RequestedEventStreamType = value; }

    /// <summary>
    /// Gets or inits the <see cref="EventStreamId"/> for the event. The getter defaults to <see cref="EventStreamId.Default"/>;
    /// leaving the property unset lets the kernel resolve its configured append default.
    /// </summary>
    public EventStreamId EventStreamId { get => RequestedEventStreamId ?? EventStreamId.Default; init => RequestedEventStreamId = value; }

    /// <summary>
    /// Gets or inits the <see cref="EventSourceType"/> for the event. The getter defaults to <see cref="EventSourceType.Default"/>;
    /// leaving the property unset lets the kernel resolve its configured append default.
    /// </summary>
    public EventSourceType EventSourceType { get => RequestedEventSourceType ?? EventSourceType.Default; init => RequestedEventSourceType = value; }

    /// <summary>
    /// Gets or inits the optional occurred time. If not set, the server will set it to approximately the time of append.
    /// </summary>
    public DateTimeOffset? Occurred { get; init; }

    /// <summary>
    /// Gets or inits the tags to associate with the event. These are combined with any static tags declared on the
    /// event type and any tags supplied at append time.
    /// </summary>
    public IEnumerable<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets the explicit stream type, preserving omission for kernel routing despite the public getter's legacy default.
    /// </summary>
    internal EventStreamType? RequestedEventStreamType { get; init; }

    /// <summary>
    /// Gets the explicit stream id, preserving omission for kernel routing despite the public getter's legacy default.
    /// </summary>
    internal EventStreamId? RequestedEventStreamId { get; init; }

    /// <summary>
    /// Gets the explicit source type, preserving omission for kernel routing despite the public getter's legacy default.
    /// </summary>
    internal EventSourceType? RequestedEventSourceType { get; init; }

    /// <summary>
    /// Compares the public event values without treating omitted routing as a different value from its legacy default.
    /// </summary>
    /// <param name="other">The event to compare with.</param>
    /// <returns>Whether both events have the same record type and public values.</returns>
    public virtual bool Equals(EventForEventSourceId? other) =>
        other is not null &&
        EqualityContract == other.EqualityContract &&
        EventSourceId == other.EventSourceId &&
        Equals(Event, other.Event) &&
        Equals(Causation, other.Causation) &&
        Subject == other.Subject &&
        EventStreamType == other.EventStreamType &&
        EventStreamId == other.EventStreamId &&
        EventSourceType == other.EventSourceType &&
        Occurred == other.Occurred &&
        EqualityComparer<IEnumerable<string>>.Default.Equals(Tags, other.Tags);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(EqualityContract);
        hashCode.Add(EventSourceId);
        hashCode.Add(Event);
        hashCode.Add(Causation);
        hashCode.Add(Subject);
        hashCode.Add(EventStreamType);
        hashCode.Add(EventStreamId);
        hashCode.Add(EventSourceType);
        hashCode.Add(Occurred);
        hashCode.Add(Tags);

        return hashCode.ToHashCode();
    }
}
