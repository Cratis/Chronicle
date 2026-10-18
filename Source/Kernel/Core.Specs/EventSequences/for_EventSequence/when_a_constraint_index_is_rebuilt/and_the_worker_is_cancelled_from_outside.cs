// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_the_worker_is_cancelled_from_outside : given.a_rebuild_cancelled_outside_the_worker
{
    async Task Because()
    {
        await CancelOutsideWorker();
        await Tick();
        await Tick();
    }

    [Fact] void should_not_fail_cancellation() => _cancellationError.ShouldBeNull();
    [Fact] async Task should_release_the_active_attempt_and_restore_the_history() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_complete_the_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldBeNull();
}
