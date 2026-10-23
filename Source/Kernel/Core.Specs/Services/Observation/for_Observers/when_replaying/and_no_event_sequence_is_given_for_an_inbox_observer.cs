// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

public class and_no_event_sequence_is_given_for_an_inbox_observer : given.all_dependencies
{
    readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer.Replay().Returns(JobId.New());
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _observerDefinitionsStorage.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true));
    }

    async Task Because() => await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = string.Empty
    });

    [Fact] void should_address_the_observers_inbox() =>
        _grainFactory.Received(1).GetGrain<IObserver>(_key);
}
