// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_the_terminal_snapshot_fails : given.a_rebuilding_constraint_index
{
    Exception? _error;

    async Task Establish()
    {
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuildStarting(_otherRunId, [_uniqueDefinition.Name]);
        _snapshots.BeforeWrite = () => Task.FromException(new IOException("Snapshot unavailable"));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten));
        _snapshots.BeforeWrite = () => Task.CompletedTask;
        await _eventSequence.ConstraintIndexRebuilt(_runId, [_uniqueDefinition.Name], ConstraintIndexRebuildOutcome.NothingWritten);
    }

    [Fact] void should_report_the_failed_snapshot() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_not_release_the_other_run_on_retry() => _snapshots.Written!.ConstraintIndexCatchUpBelow.ShouldNotBeNull();
}
