// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines the invoker for an <see cref="ReactorHandler"/>.
/// </summary>
public interface IReactorInvoker
{
    /// <summary>
    /// Gets the supported <see cref="EventType">event types</see>.
    /// </summary>
    IImmutableList<EventType> EventTypes { get; }

    /// <summary>
    /// Creates an instance of the reactor using the given service provider.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for resolving dependencies.</param>
    /// <returns>The created reactor instance.</returns>
    object CreateInstance(IServiceProvider serviceProvider);

    /// <summary>
    /// Invoke the Reactor.
    /// </summary>
    /// <param name="reactorInstance">The reactor instance to invoke on.</param>
    /// <param name="content">Event content to invoke with.</param>
    /// <param name="eventContext"><see cref="EventContext"/> for the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Invoke(object reactorInstance, object content, EventContext eventContext);
}
