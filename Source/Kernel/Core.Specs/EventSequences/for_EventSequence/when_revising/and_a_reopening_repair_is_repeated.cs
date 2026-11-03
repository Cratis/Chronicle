// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_reopening_repair_is_repeated : a_revisable_reopening_sequence
{
    async Task Because()
    {
        await ReviseAndRepair();
        _error.ShouldBeNull();
        await ReviseAndRepair();
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_keep_the_old_scope_closed() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_keep_the_new_scope_open() => (await _closures.GetFor(_closingDefinition.Name.Value, _newScope)).ShouldBeNull();
    [Fact] void should_finish_the_repair() => _snapshots.Written.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
