// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_catching_up_constraint_indexes;

public class and_a_refused_write_keeps_failing : given.a_catching_up_sequence_with_a_failing_constraint
{
    bool _admittedWhileRefusing;
    IEnumerable<ConstraintName>? _marker;
    AppendResult _refused;

    async Task Establish()
    {
        await TransferHistory();
        OccupySlots();
        _failWrites = true;
    }

    async Task Because()
    {
        for (var tick = 0; tick < 8; tick++)
        {
            if (tick > 0) _clock.Advance(TimeSpan.FromMinutes(2));
            await Tick();
        }
        _admittedWhileRefusing = WaitingAdmitted();
        _marker = _snapshots.Written!.ConstraintIndexCatchUpRefusing;
        _refused = await AppendAnEvent();
        _failWrites = false;
        _clock.Advance(TimeSpan.FromMinutes(2));
        await Tick();
    }

    [Fact] void should_keep_memory_admission_while_refusing() => _admittedWhileRefusing.ShouldBeFalse();
    [Fact] void should_keep_the_durable_refusal_marker() => _marker.ShouldContain(_uniqueDefinition.Name);
    [Fact] void should_refuse_governed_appends() => _refused.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] async Task should_restore_the_claim_once_writes_recover() => (await Values("new-holder")).ShouldContainOnly(Hash("value"));
    [Fact] void should_clear_the_refusal_after_the_claim() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
}
