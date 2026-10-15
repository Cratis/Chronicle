// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_discovering_pending_triggers;

public class with_multiple_states_per_trigger : given.an_occurrence_storage
{
    readonly TriggerId _leasedTrigger = new(Guid.NewGuid().ToString("N"));
    readonly TriggerId _finishedTrigger = new(Guid.NewGuid().ToString("N"));
    readonly TriggerId _parkedTrigger = new(Guid.NewGuid().ToString("N"));
    IEnumerable<TriggerId> _triggers;

    async Task Establish()
    {
        await _storage.Record(_occurrence);
        await _storage.Record(_occurrence with { Id = new(Guid.NewGuid().ToString("N")) });
        await _storage.Record(_occurrence with { Id = new(Guid.NewGuid().ToString("N")), Trigger = _leasedTrigger, State = OccurrenceState.Leased });
        await _storage.Record(_occurrence with { Id = new(Guid.NewGuid().ToString("N")), Trigger = _finishedTrigger, State = OccurrenceState.Completed });
        await _storage.Record(_occurrence with { Id = new(Guid.NewGuid().ToString("N")), Trigger = _parkedTrigger, State = OccurrenceState.Quarantined });
    }

    async Task Because() => _triggers = await Recreate().GetPendingTriggers();
    [Fact] void should_return_each_unfinished_trigger_once() => _triggers.ShouldContainOnly(_trigger, _leasedTrigger);
    [Fact] async Task should_find_pending_work() => (await Recreate().HasPendingForTrigger(_trigger)).ShouldBeTrue();
    [Fact] async Task should_find_leased_work() => (await Recreate().HasPendingForTrigger(_leasedTrigger)).ShouldBeTrue();
    [Fact] async Task should_ignore_completed_work() => (await Recreate().HasPendingForTrigger(_finishedTrigger)).ShouldBeFalse();
    [Fact] async Task should_ignore_quarantined_work() => (await Recreate().HasPendingForTrigger(_parkedTrigger)).ShouldBeFalse();
    [Fact] async Task should_not_match_another_trigger() => (await Recreate().HasPendingForTrigger(new(Guid.NewGuid().ToString("N")))).ShouldBeFalse();
}
