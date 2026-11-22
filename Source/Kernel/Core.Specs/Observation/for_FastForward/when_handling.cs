// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_FastForward;

public class when_handling : Specification
{
    readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    readonly FastForwardResult _fastForwarded = new(FastForwardOutcome.FastForwarded, 3UL, 10UL);
    IStorage _storage;
    IGrainFactory _grainFactory;
    IObserver _observer;
    FastForwardResult _result;

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
        _observer.FastForward().Returns(_fastForwarded);
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _result = await new FastForward("some-event-store", "some-namespace", "some-observer", string.Empty).Handle(_grainFactory, _storage);

    [Fact] void should_address_the_observer_of_the_stored_sequence() => _grainFactory.Received(1).GetGrain<IObserver>(_key);
    [Fact] void should_fast_forward_the_observer() => _observer.Received(1).FastForward();
    [Fact] void should_report_what_the_observer_reported() => _result.ShouldEqual(_fastForwarded);
}
