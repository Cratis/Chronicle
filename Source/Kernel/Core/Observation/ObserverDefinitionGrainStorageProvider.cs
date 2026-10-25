// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling observer state storage.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="enablement">Optional <see cref="IGenerationEnablement"/>; the observer identity claim is made only when generations are enabled.</param>
/// <param name="claims">Optional <see cref="IObserverIdentityClaims"/>; absent when the storage provider has no generation catalog.</param>
/// <param name="builds">Optional <see cref="IGenerationBuildStorage"/> resolving whether the observer is a generation&apos;s.</param>
public class ObserverDefinitionGrainStorageProvider(IStorage storage, IGenerationEnablement? enablement = null, IObserverIdentityClaims? claims = null, IGenerationBuildStorage? builds = null) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverDefinition>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        var observers = storage.GetEventStore(observerKey.EventStore).Observers;
        var stored = await observers.Get(observerKey.ObserverId);

        // Keep a stored event sequence as it is, so an activation under another sequence can tell that the record is
        // not its own. A missing record is on no sequence yet; the key's sequence is the one registering it claims.
        actualGrainState.State = stored with
        {
            Identifier = observerKey.ObserverId,
            EventSequenceId = stored.EventSequenceId == EventSequenceId.Unspecified ? observerKey.EventSequenceId : stored.EventSequenceId
        };
    }

    /// <inheritdoc/>
    public virtual async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var actualGrainState = (grainState as IGrainState<ObserverDefinition>)!;
        var observerKey = ObserverKey.Parse(grainId.Key.ToString()!);

        // Definitions are stored by observer identifier alone. Only an activation on the definition's own event
        // sequence may write it, so one addressed on another sequence can never overwrite the real observer's.
        var definition = actualGrainState.State!;
        if (definition.EventSequenceId != observerKey.EventSequenceId)
        {
            throw new ObserverKeyDoesNotMatchDefinition(observerKey, definition.EventSequenceId);
        }

        var observers = storage.GetEventStore(observerKey.EventStore).Observers;
        await ClaimObserverIdentity(observerKey, definition);
        await observers.Save(definition);
    }

    async Task ClaimObserverIdentity(ObserverKey observerKey, ObserverDefinition definition)
    {
        if (enablement?.Enabled != true || claims is null || builds is null) return;

        var binding = definition.Type == ObserverType.Projection
            ? await builds.ResolveOwnedObserver(claims, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId, observerKey.ObserverId)
            : null;
        if (binding is not null && !binding.Identity.Generation.IsFirst)
        {
            var generation = ObserverIdentityClaim.ForGeneration(binding);
            if (await claims.Claim(observerKey.EventStore, generation) != generation) throw new GenerationBuildConflict();
            return;
        }

        var registration = ObserverIdentityClaim.ForRegistration(observerKey.ObserverId);
        if ((await claims.Claim(observerKey.EventStore, registration)).Claimant == ObserverIdentityClaimant.Generation)
        {
            throw new ObserverIdentityClaimedByGeneration(observerKey.EventStore, observerKey.ObserverId);
        }
    }
}
