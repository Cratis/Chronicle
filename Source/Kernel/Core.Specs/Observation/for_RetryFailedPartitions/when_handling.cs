// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions;

public class when_handling : Specification
{
    readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    IStorage _storage;
    IGrainFactory _grainFactory;
    IObserver _observer;
    RetryFailedPartitionsResult _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(definitions);
        definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        _observer = Substitute.For<IObserver>();
        _observer.GetFailedPartitionKeys().Returns(Task.FromResult<IEnumerable<Key>>(
        [
            new Key("one", Properties.ArrayIndexers.NoIndexers),
            new Key("two", Properties.ArrayIndexers.NoIndexers)
        ]));
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _result = await new RetryFailedPartitions("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage);

    [Fact] void should_address_the_observers_inbox() => _grainFactory.Received(1).GetGrain<IObserver>(_key);

    [Fact] void should_tell_how_many_partitions_were_failed() => _result.FailedPartitions.ShouldEqual(2);
    [Fact] void should_ask_the_observer_to_recover_them_in_bounded_batches() => _observer.Received(1).TryRecoverAllFailedPartitions();
}
