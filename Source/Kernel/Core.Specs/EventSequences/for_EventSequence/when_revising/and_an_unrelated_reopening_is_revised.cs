// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_an_unrelated_reopening_is_revised : a_revisable_reopening_sequence
{
    protected override EventType Reopening => new("OtherReopened", EventTypeGeneration.First);

    async Task Because() => await ReviseAndRepair();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] async Task should_keep_the_old_scope_closed() => (await _closures.GetFor(_closingDefinition.Name.Value, _oldScope)).SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] async Task should_keep_the_new_scope_closed() => (await _closures.GetFor(_closingDefinition.Name.Value, _newScope)).SequenceNumber.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_not_lower_the_watermark_before_storage_revises() => _watermarkWhenStorageRevised.ShouldEqual((EventSequenceNumber)3);
}
