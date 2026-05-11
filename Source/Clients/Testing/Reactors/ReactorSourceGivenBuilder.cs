// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a builder for providing events for a specific <see cref="EventSourceId"/> to a <see cref="ReactorScenario{TReactor}"/>.
/// </summary>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
/// <param name="scenario">The <see cref="ReactorScenario{TReactor}"/> to drive.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the provided events.</param>
public class ReactorSourceGivenBuilder<TReactor>(ReactorScenario<TReactor> scenario, EventSourceId eventSourceId)
    where TReactor : class, IReactor
{
    /// <summary>
    /// Provides the given event instances to the reactor and awaits their handling.
    /// </summary>
    /// <remarks>
    /// A fresh instance of <typeparamref name="TReactor"/> is activated from the service provider and
    /// each event is passed to the reactor in order. Side-effects are observable through mocks or
    /// other test doubles registered in the service provider.
    /// </remarks>
    /// <param name="events">The event instances to provide to the reactor, in order.</param>
    /// <returns>A <see cref="Task"/> that completes when the reactor has handled all events.</returns>
    public Task Events(params object[] events) =>
        scenario.InvokeWith(eventSourceId, events);
}
