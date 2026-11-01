// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_progress_on_another_sequence : a_live_observer_with_guarded_state
{
    protected static readonly EventSequenceId _otherSequence = "another-sequence";

    protected IObserverSequenceCheckpointsStorage _checkpoints;

    async Task Establish()
    {
        _checkpoints = _eventStoreNamespaceStorage.ObserverSequenceCheckpoints;
        _eventStoreStorage.Namespaces.GetAll().Returns([new(_observerKey.Namespace, DateTimeOffset.UnixEpoch)]);
        await StoreProgressOn(_otherSequence);
    }

    protected async Task StoreProgressOn(EventSequenceId owner)
    {
        await _guarded.ClaimOwnership(_observerId, owner);
        var stored = new ObserverState
        {
            Identifier = _observerId,
            NextEventSequenceNumber = 43UL,
            LastHandledEventSequenceNumber = 42UL
        };
        await _guarded.Save(stored, owner);
    }

    protected void MoveStoredDefinitionTo(EventSequenceId eventSequenceId)
    {
        _storedDefinition = _storedDefinition with { EventSequenceId = eventSequenceId };
        _definitionStorage.State = _storedDefinition;
    }

    protected Task Subscribe() => _observer.SubscribeToAllEvents<NullObserverSubscriber>(ObserverType.Projection, SiloAddress.Zero);
}
