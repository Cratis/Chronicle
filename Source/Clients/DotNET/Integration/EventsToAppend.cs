// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents the translated events from <see cref="AdapterFor{TModel, TExternalModel}"/>.
/// </summary>
public class EventsToAppend : IEnumerable<object>
{
    readonly List<object> _events = [];

    /// <summary>
    /// Add an event.
    /// </summary>
    /// <param name="event">The actual event to append.</param>
    public void Add(object @event)
    {
        _events.Add(@event);
    }

    /// <inheritdoc/>
    public IEnumerator<object> GetEnumerator() => _events.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _events.GetEnumerator();
}
