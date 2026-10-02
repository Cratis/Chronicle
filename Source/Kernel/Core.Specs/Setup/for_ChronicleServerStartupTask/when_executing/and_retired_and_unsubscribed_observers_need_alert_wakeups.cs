// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_retired_and_unsubscribed_observers_need_alert_wakeups : given.a_startup_task
{
    IObserver _retired;
    IObserver _unsubscribed;

    void Establish()
    {
        _retired = Substitute.For<IObserver>();
        _unsubscribed = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey("retired", _eventStore, _namespace, EventSequenceId.Log)).Returns(_retired);
        _grainFactory.GetGrain<IObserver>(new ObserverKey("unsubscribed", _eventStore, _namespace, EventSequenceId.Outbox)).Returns(_unsubscribed);
        _observerStateStorage.GetAll().Returns([
            ObserverState.Empty with { Identifier = "retired", AlertDisposition = AlertDisposition.Retired, RunningState = ObserverRunningState.Quarantined },
            ObserverState.Empty with { Identifier = "unsubscribed", RunningState = ObserverRunningState.Disconnected }
        ]);
        _eventStoreStorage.Observers.GetAll().Returns([
            new ObserverDefinition { Identifier = "retired", EventSequenceId = EventSequenceId.Log },
            new ObserverDefinition { Identifier = "unsubscribed", EventSequenceId = EventSequenceId.Outbox }
        ]);
    }

    Task Because() => Execute();

    [Fact] async Task should_bootstrap_the_retired_observer() => await _retired.Received(1).Ensure();
    [Fact] async Task should_bootstrap_the_unsubscribed_observer_on_its_sequence() => await _unsubscribed.Received(1).Ensure();
}
