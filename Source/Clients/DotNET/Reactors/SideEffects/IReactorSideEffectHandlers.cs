// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Defines a system for managing <see cref="IReactorSideEffectHandler"/> instances and dispatching return values to them.
/// </summary>
public interface IReactorSideEffectHandlers
{
    /// <summary>
    /// Determines whether any registered handler can process the given return value.
    /// </summary>
    /// <param name="eventContext">The <see cref="EventContext"/> of the event that triggered the handler.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns><see langword="true"/> if any handler can process the value; otherwise <see langword="false"/>.</returns>
    bool CanHandle(EventContext eventContext, object value);

    /// <summary>
    /// Dispatches the return value to all handlers that can process it.
    /// </summary>
    /// <param name="eventContext">The <see cref="EventContext"/> of the event that triggered the handler.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> to use for appending events.</param>
    /// <param name="value">The value returned by the reactor handler method.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Handle(EventContext eventContext, IEventStore eventStore, object value);
}
