// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Reactor that observes <see cref="EventTypeGenerationAdded"/> system events and starts
/// a migration job to update existing stored events with content for the new generation.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="logger">The <see cref="ILogger{EventTypeGenerationMigrationReactor}"/> for logging.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: false, defaultNamespaceOnly: true)]
public class EventTypeGenerationMigrationReactor(IGrainFactory grainFactory, ILogger<EventTypeGenerationMigrationReactor> logger) : Reactor
{
    /// <summary>
    /// Reacts to an <see cref="EventTypeGenerationAdded"/> event by starting a migration job
    /// that updates all existing stored events of the event type with content for the new generation.
    /// </summary>
    /// <param name="event">The event containing the event type and new generation information.</param>
    /// <param name="context">The <see cref="EventContext"/> of the system event.</param>
    /// <returns>Awaitable task.</returns>
    public async Task EventTypeGenerationAdded(EventTypeGenerationAdded @event, EventContext context)
    {
        logger.StartingMigrationJob(@event.EventTypeId, @event.Generation, context.EventStore);

        var jobsManager = grainFactory.GetJobsManager(context.EventStore, context.Namespace);
        await jobsManager.Start<IMigrateExistingEventsForType, MigrateExistingEventsForTypeRequest>(
            new MigrateExistingEventsForTypeRequest(@event.EventTypeId));
    }
}
