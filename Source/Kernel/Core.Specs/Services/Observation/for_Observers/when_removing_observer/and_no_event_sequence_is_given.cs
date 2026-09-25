// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Observation;

using KernelObserverRemovalResult = Cratis.Chronicle.Concepts.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_removing_observer;

/// <summary>
/// Most observers are on the event log, and a caller that has only ever seen an observer in a listing has no reason to
/// know it has to name a sequence. An empty value resolves to the log rather than to an event sequence with no name,
/// whose grain key would address an observer that does not exist and report nothing to remove.
/// </summary>
public class and_no_event_sequence_is_given : given.all_dependencies
{
    void Establish() =>
        _observerRemover
            .Remove(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.Observation.ObserverId>(), Arg.Any<EventSequenceId>())
            .Returns(KernelObserverRemovalResult.Removed);

    async Task Because() => await _observers.RemoveObserver(new RemoveObserver
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = string.Empty
    });

    [Fact] async Task should_remove_from_the_event_log() =>
        await _observerRemover.Received(1).Remove(
            Arg.Any<Concepts.EventStoreName>(),
            Arg.Any<Concepts.Observation.ObserverId>(),
            EventSequenceId.Log);
}
