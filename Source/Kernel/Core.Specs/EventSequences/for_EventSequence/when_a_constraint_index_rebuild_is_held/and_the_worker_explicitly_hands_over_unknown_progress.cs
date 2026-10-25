// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_is_held;

public class and_the_worker_explicitly_hands_over_unknown_progress : given.a_held_constraint_index_rebuild
{
    async Task Establish() => await HoldRebuild();

    async Task Because() => await _eventSequence.ConstraintIndexRebuilt(_heldRunId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.ProgressUnknown);

    [Fact] void should_keep_the_existing_explicit_handoff_refusal_lifetime() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_preserve_the_existing_conservative_catch_up() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldNotBeNull();
}
