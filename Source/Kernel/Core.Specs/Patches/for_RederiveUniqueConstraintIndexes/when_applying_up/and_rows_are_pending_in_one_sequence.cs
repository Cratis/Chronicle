// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes.when_applying_up;

public class and_rows_are_pending_in_one_sequence : given.a_rederive_patch
{
    void Establish() => _otherStorage.CountPendingSpellings().Returns(Task.FromResult(3));

    async Task Because() => await _patch.Up();

    [Fact] void should_start_the_job_for_that_sequence_only() => _jobsManager.Received(1).Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == _otherSequence));
    [Fact] void should_not_start_the_job_for_the_event_log() => _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Is<ReindexConstraintsRequest>(_ => _.EventSequenceId == EventSequenceId.Log));
}
