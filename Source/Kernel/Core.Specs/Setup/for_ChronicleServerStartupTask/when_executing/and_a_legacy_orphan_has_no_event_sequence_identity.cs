// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_a_legacy_orphan_has_no_event_sequence_identity : given.a_startup_task
{
    void Establish() => _observerStateStorage.GetAll().Returns([ObserverState.Empty with { Identifier = "orphan", RunningState = ObserverRunningState.Quarantined }]);

    Task Because() => Execute();

    [Fact] void should_not_guess_the_event_log_and_activate_a_second_writer() => _grainFactory.DidNotReceive().GetGrain<IObserver>(new ObserverKey("orphan", _eventStore, _namespace, EventSequenceId.Log));
}
