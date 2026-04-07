// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents the entry point of the fluent builder for seeding events into a <see cref="ReadModelScenario{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">The type of read model under test.</typeparam>
/// <param name="scenario">The <see cref="ReadModelScenario{TReadModel}"/> to drive.</param>
public class ReadModelScenarioGivenBuilder<TReadModel>(ReadModelScenario<TReadModel> scenario)
    where TReadModel : class
{
    /// <summary>
    /// Specifies the <see cref="EventSourceId"/> that the events belong to.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to seed events for.</param>
    /// <returns>A <see cref="ReadModelSourceGivenBuilder{TReadModel}"/> to continue the fluent chain.</returns>
    public ReadModelSourceGivenBuilder<TReadModel> ForEventSource(EventSourceId eventSourceId) =>
        new(scenario, eventSourceId);
}
