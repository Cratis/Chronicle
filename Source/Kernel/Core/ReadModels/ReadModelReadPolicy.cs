// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Applies the producer requirements for public read-model reads.
/// </summary>
internal static class ReadModelReadPolicy
{
    /// <summary>
    /// Ensures that every projection producing the read model is supported and projection-backed models have a projection or reducer producer.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="eventStore">The event store to read.</param>
    /// <param name="definition">The read model definition.</param>
    /// <returns>The task representing the check.</returns>
    /// <exception cref="RpcException">Thrown when the projection is missing or uses an unsupported event sequence.</exception>
    internal static async Task EnsureReadable(IGrainFactory grainFactory, EventStoreName eventStore, ReadModelDefinition definition)
    {
        var manager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
        var registered = await manager.GetProjectionDefinitions();
        var stored = await manager.GetStoredProjectionDefinitions();
        var projections = registered.Concat(stored)
            .Where(_ => IsProducer(_, definition))
            .ToArray();
        var hasUnsupportedProducer = projections.Any(_ => _.UsesUnsupportedEventSequence());
        var isMissingProducer = projections.Length == 0 && definition.ObserverType == ReadModelObserverType.Projection;
        if (isMissingProducer)
        {
            var reducers = grainFactory.GetGrain<IReducersManager>(eventStore);
            isMissingProducer = !await reducers.HasReducerForReadModel(definition.Identifier);
        }
        if (hasUnsupportedProducer || isMissingProducer)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }

    static bool IsProducer(ProjectionDefinition projection, ReadModelDefinition definition) =>
        projection.ReadModel == definition.Identifier ||
        (!string.IsNullOrEmpty(definition.ObserverIdentifier.Value) && projection.Identifier.Value == definition.ObserverIdentifier.Value);
}
