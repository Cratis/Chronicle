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
    /// Collects the provided event instances for this event source to be processed together with all other
    /// collected events when <see cref="ReadModelScenario{TReadModel}.Instance"/> is first accessed.
    /// </summary>
    /// <remarks>
    /// Events are not processed immediately. Deferred processing enables multi-stream scenarios where events
    /// across different event sources are required (for example, <c>ChildrenFrom</c> projections that link
    /// child entities to parents via a parent key on a separate event source stream).
    /// </remarks>
    /// <param name="events">The event instances to collect in order.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task Events(params object[] events)
    {
        scenario.CollectEventsFor(eventSourceId, events);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers a pre-built read model instance for this event source, making it available through
    /// <see cref="ReadModelScenario{TReadModel}.ReadModels"/> for <c>GetInstanceById</c> calls.
    /// </summary>
    /// <remarks>
    /// Use this when testing production code that calls <c>IReadModels.GetInstanceById</c> — seed the
    /// expected read model state here rather than replaying events through a projection.
    /// </remarks>
    /// <param name="readModel">The read model instance to register for this event source.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task ReadModel(TReadModel readModel)
    {
        scenario.CollectReadModelFor(eventSourceId, readModel);
        return Task.CompletedTask;
    }
}
