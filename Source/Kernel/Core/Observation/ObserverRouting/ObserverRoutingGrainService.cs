// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.ObserverRouting;

/// <summary>
/// Represents an implementation of <see cref="IObserverRoutingGrainService"/>.
/// </summary>
/// <param name="grainId">The <see cref="GrainId"/> for the service.</param>
/// <param name="silo">The <see cref="Silo"/> the service belongs to.</param>
/// <param name="observers">The <see cref="IObservers"/> of the silo.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Reentrant]
public class ObserverRoutingGrainService(
    GrainId grainId,
    Silo silo,
    IObservers observers,
    ILoggerFactory loggerFactory) : GrainService(grainId, silo, loggerFactory), IObserverRoutingGrainService
{
    /// <inheritdoc/>
    public Task<ObserverDefinitionRevision> Acknowledge(EventStoreName eventStore, ObserverId observerId, ObserverDefinitionRevision revision) =>
        observers.Acknowledge(eventStore, observerId, revision);
}
