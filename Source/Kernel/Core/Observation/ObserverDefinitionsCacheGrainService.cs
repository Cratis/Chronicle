// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverDefinitionsCacheGrainService"/>.
/// </summary>
/// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
/// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
/// <param name="storage">The <see cref="IStorage"/> whose observer definition registry is told a definition changed.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Reentrant]
public class ObserverDefinitionsCacheGrainService(
    GrainId grainId,
    Silo silo,
    IStorage storage,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IObserverDefinitionsCacheGrainService
{
    /// <inheritdoc/>
    public Task Invalidate(EventStoreName eventStore, ObserverId observerId)
    {
        storage.GetEventStore(eventStore).Observers.Invalidate(observerId);
        return Task.CompletedTask;
    }
}
