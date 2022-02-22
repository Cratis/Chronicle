// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Observation;

/// <summary>
/// Defines a system for working with <see cref="IObserverMiddleware">middlewares</see> that can be called during the observing of events.
/// </summary>
public interface IObserverMiddlewares
{
    /// <summary>
    /// Invoked before the actual invoke.
    /// </summary>
    /// <param name="eventContext"><see cref="EventContext"/> for the event.</param>
    /// <param name="event">The actual event that it will be called with.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task BeforeInvoke(EventContext eventContext, object @event);

    /// <summary>
    /// Invoked after the actual invoke.
    /// </summary>
    /// <param name="eventContext"><see cref="EventContext"/> for the event.</param>
    /// <param name="event">The actual event that it will be called with.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task AfterInvoke(EventContext eventContext, object @event);
}
