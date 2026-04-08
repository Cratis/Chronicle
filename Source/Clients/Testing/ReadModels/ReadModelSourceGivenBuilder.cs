// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a builder for seeding events for a specific <see cref="EventSourceId"/> into a <see cref="ReadModelScenario{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">The type of read model under test.</typeparam>
/// <param name="scenario">The <see cref="ReadModelScenario{TReadModel}"/> to drive.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the seeded events.</param>
public class ReadModelSourceGivenBuilder<TReadModel>(ReadModelScenario<TReadModel> scenario, EventSourceId eventSourceId)
    where TReadModel : class
{
    /// <summary>
    /// Processes the provided event instances through the read model's projection or reducer and updates
    /// <see cref="ReadModelScenario{TReadModel}.Instance"/>.
    /// </summary>
    /// <param name="events">The event instances to process in order.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Events(params object[] events) =>
        scenario.ProcessEventsFor(eventSourceId, events);
}
