// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Storage.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_delivering;

public class with_a_receipted_effect : given.a_scenario
{
    OccurrenceDeliveryOutcome _outcome;
    Contracts.Occurrences.OccurrenceSnapshotResponse _snapshot;

    async Task Because()
    {
        _outcome = await _scenario.DeliverNext();
        _snapshot = (await _scenario.StatusOf(_recorded.Id))!;
    }

    [Fact] void should_complete() => _outcome.ShouldEqual(OccurrenceDeliveryOutcome.Completed);
    [Fact] void should_store_the_completion() => _snapshot.State.ShouldEqual(OccurrenceState.Completed);
    [Fact] void should_collect_the_appended_event() => _scenario.ShouldHaveAppended<EffectWasRecorded>(@event => @event.Name == "one");
    [Fact] void should_collect_the_atomic_receipt() => _scenario.ShouldHaveReceipt(new(_recorded.Id, "import", new(_recorded.Id, 1)));
    [Fact] void should_record_the_event_range() => _snapshot.Receipts.Single().Receipt.Outcome.ShouldEqual(EffectOutcome.Appended);
    [Fact] void should_use_the_scenario_recorded_time() => ((DateTimeOffset)_snapshot.RecordedAt).ShouldEqual(_scenario.Time.Now);
}
