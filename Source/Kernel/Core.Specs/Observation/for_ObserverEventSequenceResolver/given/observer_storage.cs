// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverEventSequenceResolver.given;

public class observer_storage : Specification
{
    protected IStorage _storage;
    protected IObserverDefinitionsStorage _definitions;
    protected EventSequenceId _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        _definitions = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore("some-event-store").Returns(eventStore);
        eventStore.Observers.Returns(_definitions);
        _definitions.Get("some-observer").Returns(new ObserverDefinition(
            "some-observer", [], "inbox-source", ObserverType.Reactor, ObserverOwner.Client, true));
    }
}
