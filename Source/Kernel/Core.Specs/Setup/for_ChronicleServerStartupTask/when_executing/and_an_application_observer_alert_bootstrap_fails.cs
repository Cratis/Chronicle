// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage.Observation;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_an_application_observer_alert_bootstrap_fails : given.a_startup_task_with_incomplete_persisted_capture
{
    readonly TimeoutException _bootstrapFailure = new();
    IObserver _applicationObserver;
    Exception _error;

    void Establish()
    {
        _applicationObserver = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey("unsubscribed", _eventStore, _namespace, EventSequenceId.Outbox)).Returns(_applicationObserver);
        _applicationObserver.Ensure().Returns(_ => Task.FromException(_bootstrapFailure));
        _observerStateStorage.GetAll().Returns([
            ObserverState.Empty with { Identifier = PatternCapture.ObserverIdentifier },
            ObserverState.Empty with { Identifier = "unsubscribed", RunningState = ObserverRunningState.Disconnected }
        ]);
        _eventStoreStorage.Observers.GetAll().Returns([
            new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, EventSequenceId = EventSequenceId.Log },
            new ObserverDefinition { Identifier = "unsubscribed", EventSequenceId = EventSequenceId.Outbox }
        ]);
    }

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail_startup_with_the_application_bootstrap_failure() => _error.ShouldEqual(_bootstrapFailure);
    [Fact] async Task should_exhaust_the_required_bootstrap_retry_budget() => await _applicationObserver.Received(5).Ensure();
    [Fact] async Task should_not_require_capture_alert_bootstrap() => await _captureObserver.DidNotReceive().Ensure();
    [Fact] void should_not_run_the_final_authentication_step() => _bootstrapClientsEnsured.ShouldBeFalse();
}
