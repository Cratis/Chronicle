// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_added_event_type_history_lookup : given.an_observer_automatically_reconciled_during_a_probe
{
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _query = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        var reads = 0;
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)42UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(_ =>
        {
            _definitionStorage.State = _definitionStorage.State with { EventTypes = [] };
            if (++reads == 1)
            {
                return Task.FromResult(Result<EventSequenceNumber, GetSequenceNumberError>.Success(42UL));
            }

            _probeEntered.TrySetResult();
            return _query.Task;
        });
    }

    Task Because() => QuarantineDuringProbe(ReconcileSubscription(), () => _query.SetResult(Result<EventSequenceNumber, GetSequenceNumberError>.Success(EventSequenceNumber.First)));

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
