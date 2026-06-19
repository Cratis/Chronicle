// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines a system that resolves the arguments to pass to a reactor handler method.
/// </summary>
/// <remarks>
/// The first parameter of a handler method is always the event it reacts to. Any further parameters are
/// resolved as dependencies: an <see cref="EventContext"/> parameter receives the event context, a read
/// model parameter is materialized strongly consistent from its reducer or projection, and any other
/// parameter is resolved from the <see cref="IServiceProvider"/>.
/// </remarks>
public interface IReactorMethodArgumentsResolver
{
    /// <summary>
    /// Resolve the arguments for invoking a reactor handler method.
    /// </summary>
    /// <param name="method">The <see cref="MethodInfo"/> of the handler method to resolve arguments for.</param>
    /// <param name="reactor">The reactor instance the method belongs to.</param>
    /// <param name="event">The deserialized event that triggered the reactor.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> for the triggering event.</param>
    /// <param name="eventStore">The <see cref="IEventStore"/> used to detect and materialize read model parameters.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve any other dependencies.</param>
    /// <returns>The arguments to pass to the handler method, in declaration order.</returns>
    Task<object?[]> Resolve(
        MethodInfo method,
        object reactor,
        object @event,
        EventContext eventContext,
        IEventStore? eventStore,
        IServiceProvider? serviceProvider);
}
