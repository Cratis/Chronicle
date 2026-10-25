// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_storage_revise_fails : a_revisable_sequence
{
    void Establish() => _failRevise = true;

    async Task Because() => await ReviseFirstEvent();

    [Fact] void should_report_the_failure() => _error.ShouldBeOfExactType<for_EventSequence.given.SimulatedStateWriteError>();
    [Fact] void should_keep_the_lowered_watermark() => _stateStorage.State.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_have_persisted_the_lowered_watermark() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First);
}
