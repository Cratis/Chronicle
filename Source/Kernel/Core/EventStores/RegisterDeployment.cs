// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Registrations;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Registers a deployment: 0 bypasses the generation check, lower generations are rejected, and equal or higher generations
/// are accepted. The fingerprint is recorded as the latest registered one.
/// </summary>
/// <param name="EventStore">The existing event store.</param>
/// <param name="Generation">The deployment generation, or 0 for no check.</param>
/// <param name="Fingerprint">The opaque artifact fingerprint.</param>
[Command]
[BelongsTo(WellKnownServices.EventStores)]
public record RegisterDeployment(EventStoreName EventStore, DeploymentGeneration Generation, ArtifactFingerprint Fingerprint)
{
    /// <summary>
    /// Checks and records the deployment through the store's serialized registry.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>A member-scoped error for stale generations, otherwise null.</returns>
    public async Task<ValidationResult?> Handle(IGrainFactory grainFactory)
    {
        var decision = await grainFactory.GetGrain<IEventStoreDeploymentRegistry>(EventStore.Value).Register(Generation, Fingerprint);
        return decision.Accepted
            ? null
            : ValidationResult.Error($"Deployment generation {Generation} for event store '{EventStore}' is lower than the stored generation {decision.Stored.Generation}.", [nameof(Generation)]);
    }
}
