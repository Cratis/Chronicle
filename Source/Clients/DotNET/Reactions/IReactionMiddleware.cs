// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Defines a middleware that can be called during the observing of events.
/// </summary>
public interface IReactionMiddleware
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
