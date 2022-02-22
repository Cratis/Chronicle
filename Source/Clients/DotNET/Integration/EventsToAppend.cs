// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Aksio.Cratis.Integration;

/// <summary>
/// Represents the translated events from <see cref="AdapterFor{TModel, TExternalModel}"/>.
/// </summary>
public class EventsToAppend : IEnumerable, IEnumerable<object>
{
    readonly List<object> _events = new();

    /// <summary>
    /// Add an event.
    /// </summary>
    /// <param name="event">Event to add.</param>
    public void Add(object @event)
    {
        _events.Add(@event);
    }

    /// <inheritdoc/>
    public IEnumerator GetEnumerator() => _events.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<object> IEnumerable<object>.GetEnumerator() => _events.GetEnumerator();
}
