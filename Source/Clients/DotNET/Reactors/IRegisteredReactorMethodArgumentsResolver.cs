// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Resolves handler arguments with the owning registration's delivery identity.
/// </summary>
internal interface IRegisteredReactorMethodArgumentsResolver
{
    /// <summary>
    /// Resolves arguments without re-deriving registered identities from declarations.
    /// </summary>
    /// <param name="method">The handler method.</param>
    /// <param name="reactor">The activated reactor.</param>
    /// <param name="event">The triggering event.</param>
    /// <param name="eventContext">The triggering context.</param>
    /// <param name="eventStore">The owning store.</param>
    /// <param name="serviceProvider">The activation scope's services.</param>
    /// <param name="delivery">The registered delivery identity.</param>
    /// <returns>The handler arguments.</returns>
    Task<object?[]> Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider,
        ReactorDelivery delivery);
}
