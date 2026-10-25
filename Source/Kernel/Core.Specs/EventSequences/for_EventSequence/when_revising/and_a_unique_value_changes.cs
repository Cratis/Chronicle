// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_unique_value_changes : a_revisable_sequence
{
    async Task Because() => await ReviseFirstEvent();

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_have_persisted_the_lowered_watermark_before_storage_revised() => _watermarkWhenStorageRevised.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_keep_the_lowered_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
}
