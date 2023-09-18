// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Defines a system that can handle events from an <see cref="IObserver"/>.
/// </summary>
public interface IObserverEventHandler
{
    /// <summary>
    /// Handle an event.
    /// </summary>
    /// <param name="event">The <see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(AppendedEvent @event);
}
