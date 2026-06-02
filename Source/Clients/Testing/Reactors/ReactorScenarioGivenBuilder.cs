// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents the entry point of the fluent builder for providing events to a <see cref="ReactorScenario{TReactor}"/>.
/// </summary>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
/// <param name="scenario">The <see cref="ReactorScenario{TReactor}"/> to drive.</param>
public class ReactorScenarioGivenBuilder<TReactor>(ReactorScenario<TReactor> scenario)
    where TReactor : class, IReactor
{
    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> that the events originate from.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the events.</param>
    /// <returns>A <see cref="ReactorSourceGivenBuilder{TReactor}"/> to continue the fluent chain.</returns>
    public ReactorSourceGivenBuilder<TReactor> ForEventSource(EventSourceId eventSourceId) =>
        new(scenario, eventSourceId);
}
