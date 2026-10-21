// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_repair_snapshot_is_still_in_flight : given.a_catching_up_sequence
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _snapshotsStarted;
    int _writesBeforeCatchUp;
    protected override EventSequenceNumber? Watermark => EventSequenceNumber.First;

    async Task Establish()
    {
        await Seed(0, "first", email: "a");
        await Seed(1, "second", email: "b");
        await Seed(2, "third", email: "c");
        _snapshots.BeforeWrite = () =>
        {
            _snapshotsStarted++;
            if (_snapshotsStarted != 1) return Task.CompletedTask;
            _clock.Advance(TimeSpan.FromMilliseconds(250));
            return _release.Task;
        };
        _stateStorage.State.ConstraintIndexCatchUpBelow = 3;
        await Tick();
        _writesBeforeCatchUp = _claimWrites;
        await _claims.Remove("third", _uniqueDefinition);
        _failWrites = true;
    }

    async Task Because()
    {
        var turn = Tick();
        _clock.Advance(TimeSpan.FromMilliseconds(250));
        await turn;
    }

    void Destroy() => _release.TrySetResult();

    [Fact] void should_not_issue_a_barrier_snapshot_ahead_of_the_old_repair_snapshot() => _snapshotsStarted.ShouldEqual(1);
    [Fact] void should_not_write_claims_before_the_queued_barrier_is_durable() => _claimWrites.ShouldEqual(_writesBeforeCatchUp);
}
