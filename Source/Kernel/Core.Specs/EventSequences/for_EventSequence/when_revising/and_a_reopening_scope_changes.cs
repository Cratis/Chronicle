// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_reopening_scope_changes : a_revisable_reopening_sequence
{
    async Task Because() => await ReviseAndRepair();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_restore_the_old_scope_closure() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_reopen_the_new_scope() => (await _closures.GetFor(_closingDefinition.Name.Value, _newScope)).ShouldBeNull();
    [Fact] void should_persist_the_full_refold_before_storage_revises() => _watermarkWhenStorageRevised.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_finish_the_repair() => _snapshots.Written.ConstraintIndexWatermark.ShouldEqual((EventSequenceNumber)3);
}
