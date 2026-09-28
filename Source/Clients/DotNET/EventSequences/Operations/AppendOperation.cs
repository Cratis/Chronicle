// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Operations;

/// <summary>
/// Represents an operation to append an event to an event sequence.
/// </summary>
/// <param name="Event">The event to append.</param>
/// <param name="Causation">Optional The causation for the event.</param>
/// <param name="EventStreamType">Optional The type of event stream.</param>
/// <param name="EventStreamId">Optional The identifier for the event stream.</param>
/// <param name="EventSourceType">Optional The type of event source.</param>
/// <param name="Tags">Optional dynamic tags to associate with the event.</param>
/// <param name="Occurred">Optional timestamp for when the event occurred.</param>
/// <param name="Subject">Optional subject identifying what the event is about.</param>
public record AppendOperation(
    object Event,
    Causation? Causation = default,
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default,
    EventSourceType? EventSourceType = default,
    IEnumerable<string>? Tags = default,
    DateTimeOffset? Occurred = default,
    Subject? Subject = default) : IEventSequenceOperation
{
    /// <summary>
    /// Gets or inits the structured named tags for this event.
    /// </summary>
    /// <exception cref="InvalidNamedTag">The collection contains a null named tag.</exception>
    public IEnumerable<NamedTag> NamedTags
    {
        get;
        init
        {
            if (value is null)
            {
                field = [];
                return;
            }

            var snapshot = value.ToArray();
            if (snapshot.Any(tag => tag is null))
            {
                throw new InvalidNamedTag();
            }

            field = snapshot.Length == 0 ? Array.Empty<NamedTag>() : Array.AsReadOnly(snapshot);
        }
    } = [];
}
