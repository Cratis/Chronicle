// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Registrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Serializes deployment checks and appends across the cluster for one event store.
/// </summary>
/// <param name="storage">The storage containing deployment events.</param>
/// <param name="eventTypes">The event types to register before appending.</param>
/// <param name="eventSerializer">The serializer for stored deployment events.</param>
public class EventStoreDeploymentRegistry(IStorage storage, IEventTypes eventTypes, IEventSerializer eventSerializer) : Grain, IEventStoreDeploymentRegistry
{
    DeploymentState _state = DeploymentState.Empty;
    bool _eventTypesRegistered;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var name = (EventStoreName)this.GetPrimaryKeyString();
        var last = await storage.GetEventStore(EventStoreName.System)
            .GetNamespace(EventStoreNamespaceName.Default)
            .GetEventSequence(EventSequenceId.System)
            .TryGetLastInstanceOfAny(name.Value, [typeof(EventStoreDeploymentRegistered).GetEventType().Id]);
        if (last.TryGetValue(out var appended))
        {
            var registered = (EventStoreDeploymentRegistered)eventSerializer.Deserialize(appended);
            _state = new(registered.Generation, registered.Fingerprint);
        }
    }

    /// <inheritdoc/>
    public async Task<DeploymentDecision> Register(DeploymentGeneration generation, ArtifactFingerprint fingerprint)
    {
        var decision = DeploymentDecision.Decide(_state, generation, fingerprint);
        if (decision.ToRecord is not { } next) return decision;

        if (!_eventTypesRegistered)
        {
            await eventTypes.DiscoverAndRegister(EventStoreName.System);
            _eventTypesRegistered = true;
        }

        var name = (EventStoreName)this.GetPrimaryKeyString();
        var result = await GrainFactory.GetSystemEventSequence(EventStoreName.System)
            .Append(name.Value, new EventStoreDeploymentRegistered(name, next.Generation, next.Fingerprint));
        if (!result.IsSuccess) throw new EventStoreDeploymentRegistrationFailed(name);

        _state = next;
        return decision;
    }

    /// <inheritdoc/>
    public Task<DeploymentState> Get() => Task.FromResult(_state);
}
