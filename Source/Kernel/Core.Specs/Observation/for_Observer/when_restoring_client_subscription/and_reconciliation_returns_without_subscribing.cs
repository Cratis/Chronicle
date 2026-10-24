// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_reconciliation_returns_without_subscribing : given.an_observer_that_lost_its_client
{
    readonly RecordingLogger<Observer> _recordedLogs = new();
    readonly SiloAddress _otherSilo = SiloAddress.FromParsableString("127.0.0.1:11111@1");

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    async Task Because()
    {
        await RestoreTwice(SiloAddress.Zero, ObserverRunningState.Disconnected);
        await RestoreTwice(_otherSilo, ObserverRunningState.Disconnected);
        await RestoreTwice(_otherSilo, ObserverRunningState.Active);
    }

    [Fact] void should_suppress_repeated_attempts_with_the_same_target_and_state() => Messages("Restore attempt diagnostic changed").Count().ShouldEqual(3);
    [Fact] void should_suppress_repeated_results_even_as_attempt_and_result_alternate() => Messages("Restore result diagnostic changed").Count().ShouldEqual(2);
    [Fact] void should_report_the_returned_state_without_claiming_subscription() => Messages("Restore result diagnostic changed")
        .All(_ => _.Contains("Quarantined", StringComparison.Ordinal) && _.Contains("subscribed False", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_not_claim_to_know_the_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();

    async Task RestoreTwice(SiloAddress silo, ObserverRunningState state)
    {
        await Restore(silo, state);
        await Restore(silo, state);
    }

    Task Restore(SiloAddress silo, ObserverRunningState state)
    {
        _stateStorage.State = _stateStorage.State with { RunningState = state };
        return _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, EventsWithLifecycleChange(), silo, _client);
    }

    IEnumerable<EventType> EventsWithLifecycleChange()
    {
        // Reconciliation must still honor a lifecycle change after restoration's initial guards.
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Quarantined };
        yield return event_type;
    }

    IEnumerable<string> Messages(string marker) => _recordedLogs.Messages.Where(_ => _.Contains(marker, StringComparison.Ordinal));
}
