// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_reopening_scope_is_unchanged : a_revisable_reopening_sequence
{
    protected override string RevisedScope => "claimed@example.com";

    async Task Because() => await ReviseAndRepair();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_keep_the_original_scope_open() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).ShouldBeNull();
    [Fact] async Task should_keep_the_other_scope_closed() => (await _closures.GetFor(_closingDefinition.Name.Value, _newScope)).SequenceNumber.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_finish_the_repair() => _snapshots.Written.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
