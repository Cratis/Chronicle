// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents event input from one source to a fluent reactor scenario.
/// </summary>
/// <param name="scenario">The scenario to drive.</param>
/// <param name="eventSourceId">The source of the events.</param>
public class ReactorSourceGivenBuilder(ReactorScenario scenario, EventSourceId eventSourceId)
{
    /// <summary>
    /// Provides events in order and awaits every applicable callback.
    /// </summary>
    /// <param name="events">The events to provide.</param>
    /// <returns>A task completing after all callbacks complete.</returns>
    public Task Events(params object[] events) => scenario.InvokeWith(eventSourceId, events);
}
