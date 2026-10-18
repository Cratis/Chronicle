// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_index_rebuild_starts;

public class and_an_unrelated_catch_up_clears_its_refusal : given.a_rebuilding_constraint_index
{
    async Task Establish()
    {
        _stateStorage.State.ConstraintIndexCatchUpBelow = 2;
        _stateStorage.State.ConstraintIndexCatchUpRefusing = [_uniqueDefinition.Name, _otherDefinition.Name];
        await _eventSequence.ConstraintIndexRebuildStarting(_runId, [_uniqueDefinition.Name]);
    }

    async Task Because()
    {
        // Exercise the callback supplied to the replay when its unrelated writes have completed.
        await (Task)typeof(EventSequence).GetMethod("ClearCatchUpRefusal", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_eventSequence, [CancellationToken.None])!;
    }

    [Fact] void should_persist_only_the_rebuilding_constraint_refusal() => _snapshots.Written!.ConstraintIndexCatchUpRefusing!.ShouldContainOnly(_uniqueDefinition.Name);
}
