// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for specifications.
/// </summary>
public class EventOutboxForSpecifications : IEventOutbox
{
    readonly EventSequenceForSpecifications _sequence = new();

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _sequence.AppendedEvents;

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event) => _sequence.Append(eventSourceId, @event);
}
