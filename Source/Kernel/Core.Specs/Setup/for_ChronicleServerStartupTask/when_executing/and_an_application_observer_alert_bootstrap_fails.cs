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
    const int FailedActivations = 6;

    IObserver _applicationObserver;
    StartupWorkItem _observerWork;
    int _activations;
    Exception _error;

    void Establish()
    {
        _applicationObserver = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(new ObserverKey("unsubscribed", _eventStore, _namespace, EventSequenceId.Outbox)).Returns(_applicationObserver);
        _observerWork = new("ActivateObserver", new ObserverKey("unsubscribed", _eventStore, _namespace, EventSequenceId.Outbox).ToString());
        _applicationObserver.Ensure().Returns(_ => ++_activations <= FailedActivations ? Task.FromException(new TimeoutException()) : Task.CompletedTask);
        _observerStateStorage.GetAll().Returns([
            ObserverState.Empty with { Identifier = PatternCapture.ObserverIdentifier },
            ObserverState.Empty with { Identifier = "unsubscribed", RunningState = ObserverRunningState.Disconnected }
        ]);
        _eventStoreStorage.Observers.GetAll().Returns([
            new ObserverDefinition { Identifier = PatternCapture.ObserverIdentifier, EventSequenceId = EventSequenceId.Log },
            new ObserverDefinition { Identifier = "unsubscribed", EventSequenceId = EventSequenceId.Outbox }
        ]);
    }

    async Task Because() => _error = await Catch.Exception(ExecuteAndSettle);

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] async Task should_keep_retrying_the_bootstrap_until_it_succeeds() => await _applicationObserver.Received(FailedActivations + 1).Ensure();
    [Fact] void should_escalate_through_the_seam() => _escalation.Escalations.Select(_ => (_.Item, _.Attempts)).ShouldContainOnly([(_observerWork, RecoverableStartupWork.EscalateAfterFailures)]);
    [Fact] async Task should_not_require_capture_alert_bootstrap() => await _captureObserver.DidNotReceive().Ensure();
    [Fact] void should_ensure_bootstrap_clients() => _bootstrapClientsEnsured.ShouldBeTrue();
}
