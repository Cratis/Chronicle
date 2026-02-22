// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines the invoker for an <see cref="ReactorHandler"/>.
/// </summary>
public interface IReactorInvoker
{
    /// <summary>
    /// Invoke the Reactor.
    /// </summary>
    /// <param name="content">Event content to invoke with.</param>
    /// <param name="eventContext"><see cref="EventContext"/> for the event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task<Catch> Invoke(object content, EventContext eventContext);
}
