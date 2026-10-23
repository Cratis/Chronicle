// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

using KernelObserverRemovalResult = Cratis.Chronicle.Concepts.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_removing_observer;

/// <summary>
/// An omitted sequence follows the stored observer definition rather than assuming the event log.
/// </summary>
public class and_no_event_sequence_is_given : given.all_dependencies
{
    readonly EventSequenceId _eventSequenceId = "inbox-source";

    void Establish()
    {
        _observerDefinitionsStorage.Get("some-observer").Returns(new ObserverDefinition(
            "some-observer", [], _eventSequenceId, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, true));
        _observerRemover
            .Remove(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.Observation.ObserverId>(), Arg.Any<EventSequenceId>())
            .Returns(KernelObserverRemovalResult.Removed);
    }

    async Task Because() => await _observers.RemoveObserver(new RemoveObserver
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = string.Empty
    });

    [Fact] async Task should_remove_from_the_observers_inbox() =>
        await _observerRemover.Received(1).Remove(
            Arg.Any<Concepts.EventStoreName>(),
            Arg.Any<Concepts.Observation.ObserverId>(),
            _eventSequenceId);
}
