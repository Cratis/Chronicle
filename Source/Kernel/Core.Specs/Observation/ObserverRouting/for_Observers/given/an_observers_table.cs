// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_Observers.given;

public class an_observers_table : Specification
{
    protected static readonly EventStoreName _eventStore = "an-event-store";
    protected static readonly ObserverId _observerId = "an-observer";

    protected StateProvider _stateProvider;
    protected Observers _observers;

    void Establish()
    {
        _stateProvider = new();
        _observers = new(_stateProvider);
    }

    protected static ObserverDefinitionRegistrySnapshot SnapshotWith(long revision, params ObserverDefinition[] definitions) =>
        new(revision, definitions.ToFrozenDictionary(_ => _.Identifier));

    protected static ObserverDefinition DefinitionAt(ObserverDefinitionRevision revision) =>
        new(_observerId, EventTypeSet.From([new EventType("an-event", 1)]), EventSequenceId.Log, ObserverType.Reactor, ObserverOwner.Client, true) { Revision = revision };

    /// <summary>
    /// A state provider that holds one snapshot until it is told a definition changed, and the stored one after that.
    /// </summary>
    protected sealed class StateProvider : IObserverDefinitionsStateProvider
    {
        public ObserverDefinitionRegistrySnapshot Held { get; set; } = ObserverDefinitionRegistrySnapshot.Empty;

        public ObserverDefinitionRegistrySnapshot Stored { get; set; } = ObserverDefinitionRegistrySnapshot.Empty;

        public int Changes { get; private set; }

        public ValueTask<ObserverDefinitionRegistrySnapshot> GetSnapshot(EventStoreName eventStore) => ValueTask.FromResult(Changes > 0 ? Stored : Held);

        public Task<ObserverDefinitionRevision> GetStoredRevision(EventStoreName eventStore, ObserverId observerId) =>
            Task.FromResult(Stored.TryGetDefinition(observerId, out var definition) ? definition.Revision : ObserverDefinitionRevision.None);

        public void Changed(EventStoreName eventStore, ObserverId observerId) => Changes++;
    }
}
