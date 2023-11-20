// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that can handle events for an <see cref="IAggregateRoot"/>.
/// </summary>
public interface IAggregateRootEventHandlers
{
    /// <summary>
    /// Gets whether or not it has any handle methods.
    /// </summary>
    public bool HasHandleMethods { get; }

    /// <summary>
    /// Gets a collection of <see cref="EventType">event types</see> that can be handled.
    /// </summary>
    IImmutableList<EventType> EventTypes { get; }

    /// <summary>
    /// Handle a collection of events.
    /// </summary>
    /// <param name="target">The target <see cref="IAggregateRoot"/> to handle for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events);
}
