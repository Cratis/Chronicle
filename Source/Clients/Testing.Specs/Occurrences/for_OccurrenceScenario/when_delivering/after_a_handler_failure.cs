// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_delivering;

public class after_a_handler_failure : given.a_scenario
{
    OccurrenceDeliveryOutcome _failed;
    OccurrenceDeliveryOutcome _beforeDue;
    OccurrenceDeliveryOutcome _retried;
    DateTimeOffset _nextAttempt;

    async Task Because()
    {
        _failed = await _scenario.DeliverNextFailing(new ScenarioFailure());
        var snapshot = (await _scenario.StatusOf(_recorded.Id))!;
        _nextAttempt = snapshot.NextAttempt;
        _beforeDue = await _scenario.DeliverNext();
        _scenario.Time.AdvanceTo(_nextAttempt);
        _retried = await _scenario.DeliverNext();
    }

    [Fact] void should_report_failure() => _failed.ShouldEqual(OccurrenceDeliveryOutcome.Failed);
    [Fact] void should_wait_for_backoff() => _beforeDue.ShouldEqual(OccurrenceDeliveryOutcome.NothingDue);
    [Fact] void should_use_the_one_second_initial_backoff() => _nextAttempt.ShouldEqual(new ScenarioTime().Now.AddSeconds(1));
    [Fact] void should_deliver_after_explicit_time_advance() => _retried.ShouldEqual(OccurrenceDeliveryOutcome.Completed);

    sealed class ScenarioFailure : Exception;
}
