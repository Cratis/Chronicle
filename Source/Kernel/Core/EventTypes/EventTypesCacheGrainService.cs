// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesCacheGrainService"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypesCacheGrainService"/> class.
/// </remarks>
/// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
/// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
/// <param name="storage">The <see cref="IStorage"/> whose per-silo event type cache is evicted.</param>
/// <param name="schemaCache">The <see cref="IEventTypeSchemaCache"/> whose per-silo schema cache is evicted.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Reentrant]
public class EventTypesCacheGrainService(
    GrainId grainId,
    Silo silo,
    IStorage storage,
    IEventTypeSchemaCache schemaCache,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IEventTypesCacheGrainService
{
    /// <inheritdoc/>
    public Task Invalidate(EventStoreName eventStore, EventTypeId eventTypeId)
    {
        storage.GetEventStore(eventStore).EventTypes.Invalidate(eventTypeId);
        schemaCache.Invalidate(eventStore, eventTypeId);
        return Task.CompletedTask;
    }
}
