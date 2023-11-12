// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Defines a system that can manage state for an <see cref="AggregateRoot"/>.
/// </summary>
public interface IAggregateRootStateManager
{
    /// <summary>
    /// Handle state for an <see cref="AggregateRoot"/>.
    /// </summary>
    /// <param name="aggregateRoot">The <see cref="AggregateRoot"/> to handle state for.</param>
    /// <param name="events">The events to handle state for.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AggregateRoot aggregateRoot, IEnumerable<AppendedEvent> events);
}
