// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationPhase;

public class when_checking_persisted_values : Specification
{
    [Fact] void should_persist_none_as_zero() => ((int)EventSequenceMutationPhase.None).ShouldEqual(0);
    [Fact] void should_persist_reserved_as_one() => ((int)EventSequenceMutationPhase.Reserved).ShouldEqual(1);
    [Fact] void should_persist_applying_as_two() => ((int)EventSequenceMutationPhase.Applying).ShouldEqual(2);
    [Fact] void should_persist_verifying_as_three() => ((int)EventSequenceMutationPhase.Verifying).ShouldEqual(3);
    [Fact] void should_persist_blocked_as_four() => ((int)EventSequenceMutationPhase.Blocked).ShouldEqual(4);
    [Fact] void should_persist_source_committed_as_five() => ((int)EventSequenceMutationPhase.SourceCommitted).ShouldEqual(5);
}
