// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents the event history builder for a fluent reactor scenario.
/// </summary>
/// <param name="scenario">The scenario to drive.</param>
public class ReactorScenarioGivenBuilder(ReactorScenario scenario)
{
    /// <summary>
    /// Specifies the source of the provided events.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <returns>The builder for providing events.</returns>
    public ReactorSourceGivenBuilder ForEventSource(EventSourceId eventSourceId) => new(scenario, eventSourceId);
}
