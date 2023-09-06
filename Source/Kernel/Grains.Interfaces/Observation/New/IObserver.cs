// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Defines a system that can observe events.
/// </summary>
public interface IObserver : IGrainWithGuidKey
{
    /// <summary>
    /// Called when an event occurs.
    /// </summary>
    /// <param name="event">The event that occurred.</param>
    /// <returns>Awaitable task.</returns>
    Task OnEvent(AppendedEvent @event);
}
