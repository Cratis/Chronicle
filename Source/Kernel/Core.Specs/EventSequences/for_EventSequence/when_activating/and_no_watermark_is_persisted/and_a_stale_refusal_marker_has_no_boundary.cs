// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_a_stale_refusal_marker_has_no_boundary : given.a_catching_up_sequence
{
    async Task Establish()
    {
        _stateStorage.State.ConstraintIndexCatchUpBelow = null;
        _stateStorage.State.ConstraintIndexCatchUpRefusing = [_uniqueDefinition.Name];
        await _log.SaveState(_stateStorage.State);
    }

    async Task Because()
    {
        await Reactivate();
        _result = await AppendAnEvent();
    }

    [Fact] void should_clear_the_stale_marker() => _snapshots.Written!.ConstraintIndexCatchUpRefusing.ShouldBeNull();
    [Fact] void should_not_refuse_a_governed_append() => _result.IsSuccess.ShouldBeTrue();
}
