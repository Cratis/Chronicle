// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Integration;

/// <summary>
/// Represents the translated events from <see cref="AdapterFor{TModel, TExternalModel}"/>.
/// </summary>
public class EventsToAppend : IEnumerable<EventToAppend>
{
    readonly List<EventToAppend> _events = new();

    /// <summary>
    /// Add an event.
    /// </summary>
    /// <param name="event">The actual event to append.</param>
    /// <param name="validFrom">Optional date and time for when the event is valid from. </param>
    public void Add(object @event, DateTimeOffset? validFrom = default)
    {
        _events.Add(new(@event, validFrom));
    }

    /// <inheritdoc/>
    public IEnumerator<EventToAppend> GetEnumerator() => _events.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _events.GetEnumerator();
}
