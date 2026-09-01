// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationKind;

public class when_checking_persisted_values : Specification
{
    [Fact] void should_persist_unknown_as_zero() => ((int)EventSequenceMutationKind.Unknown).ShouldEqual(0);
    [Fact] void should_persist_revision_as_one() => ((int)EventSequenceMutationKind.Revision).ShouldEqual(1);
    [Fact] void should_persist_point_redaction_as_two() => ((int)EventSequenceMutationKind.PointRedaction).ShouldEqual(2);
    [Fact] void should_persist_event_source_redaction_as_three() => ((int)EventSequenceMutationKind.EventSourceRedaction).ShouldEqual(3);
    [Fact] void should_persist_generation_backfill_as_four() => ((int)EventSequenceMutationKind.GenerationBackfill).ShouldEqual(4);
}
