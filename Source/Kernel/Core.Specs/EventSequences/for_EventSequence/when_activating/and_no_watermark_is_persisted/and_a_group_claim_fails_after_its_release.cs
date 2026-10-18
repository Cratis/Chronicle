// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_group_claim_fails_after_its_release : given.a_catching_up_sequence
{
    AppendResult _refused;
    int _attemptsBeforeRetry;
    bool _markerPersisted;
    int _snapshotsAtRestoredClaim;

    async Task Establish()
    {
        await TransferHistory();
        _failWrites = true;
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _claimWrites++;
                if (_failWrites) return Task.FromException(new given.SimulatedStateWriteError());
                _snapshotsAtRestoredClaim = _snapshots.Snapshots.Count;
                return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));
            });
    }

    async Task Because()
    {
        await Tick();
        _attemptsBeforeRetry = _claimWrites;
        _markerPersisted = _snapshots.Written!.ConstraintIndexCatchUpRefusing?.Contains(_uniqueDefinition.Name) == true;
        _refused = await AppendAnEvent();
        _failWrites = false;
        _clock.Advance(TimeSpan.FromSeconds(1));
        await Tick();
    }

    [Fact] void should_attempt_the_failed_claim_only_once_in_the_turn() => _attemptsBeforeRetry.ShouldEqual(1);
    [Fact] void should_refuse_governed_appends() => _refused.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_keep_the_durable_refusal_marker() => _markerPersisted.ShouldBeTrue();
    [Fact] async Task should_restore_the_claim_on_a_later_turn() => (await Values("new-holder")).ShouldContain(Hash("value"));
    [Fact] void should_persist_refusal_through_the_restored_claim() => _snapshots.Snapshots.Skip(1).Take(_snapshotsAtRestoredClaim - 1).ShouldEachConformTo(snapshot => snapshot.ConstraintIndexCatchUpRefusing?.Contains(_uniqueDefinition.Name) == true);
    [Fact] void should_clear_refusal_only_after_restoring_the_claim() => _snapshots.Snapshots.Skip(_snapshotsAtRestoredClaim).ShouldContain(snapshot => snapshot.ConstraintIndexCatchUpRefusing == null);
    [Fact] void should_exercise_persisted_barriers_before_the_claim() => _snapshotsAtRestoredClaim.ShouldBeGreaterThan(1);
}
