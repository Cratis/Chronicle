// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_is_rebuilt;

public class and_a_failed_step_is_retried : given.a_rebuilding_constraint_index
{
    ConstraintIndexRebuildRunId _retry;
    UniqueConstraintValue[] _during;

    async Task Establish()
    {
        var first = new ConstraintIndexRebuildRunId(_runId.Value, Guid.NewGuid());
        await _eventSequence.ConstraintIndexRebuildStarting(first, [_uniqueDefinition.Name]);
        await _eventSequence.ConstraintIndexRebuilt(first, [_uniqueDefinition.Name], new(true, 0));
        _retry = new(_runId.Value, Guid.NewGuid());
        await _eventSequence.ConstraintIndexRebuildStarting(_retry, [_uniqueDefinition.Name]);
        await _claims.ClearValues(_uniqueDefinition);

        // A duplicate Starting from the finished attempt must not replace the retry.
        await _eventSequence.ConstraintIndexRebuildStarting(first, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        await Tick();
        _during = await Values("source");
        await _eventSequence.ConstraintIndexRebuilt(_retry, [_uniqueDefinition.Name], new(true, 0));
    }

    [Fact] void should_exclude_the_retry_from_background_writes() => _during.ShouldBeEmpty();
    [Fact] async Task should_fold_the_retried_attempt() => (await Values("source")).ShouldContainOnly(Hash("current"));
    [Fact] void should_advance_the_watermark() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)2);
}
