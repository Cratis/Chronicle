// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
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
    /// Ensures that a projection-backed read model has a supported producer.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <param name="eventStore">The event store to read.</param>
    /// <param name="definition">The read model definition.</param>
    /// <returns>The task representing the check.</returns>
    /// <exception cref="RpcException">Thrown when the projection is missing or uses an unsupported event sequence.</exception>
    internal static async Task EnsureReadable(IGrainFactory grainFactory, EventStoreName eventStore, ReadModelDefinition definition)
    {
        if (definition.ObserverType != ReadModelObserverType.Projection) return;
        var manager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
        var projections = await manager.GetProjectionDefinitions();
        var projection = projections.FirstOrDefault(_ => _.Identifier.Value == definition.ObserverIdentifier.Value);

        // A registration can still be in flight or have failed after persisting the definition, so the stored one decides.
        projection ??= await manager.GetStoredProjectionDefinition((ProjectionId)definition.ObserverIdentifier.Value);
        if (projection is null || projection.EventSequenceId.IsSystemSequence())
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, SystemStoreAccess.Refused));
        }
    }
}
