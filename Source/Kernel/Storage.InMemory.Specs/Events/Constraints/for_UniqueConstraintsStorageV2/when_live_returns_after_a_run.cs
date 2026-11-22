// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_live_returns_after_a_run : given.an_authority_refusal
{
    async Task Establish()
    {
        await Transition(Run(1));
        await Transition(UniqueConstraintWritePhase.Live);
    }

    async Task Because() => await Refuse();
    [Fact] void should_prevent_aba_for_all_six_old_live_mutations() => AllRefused.ShouldBeTrue();
    [Fact] void should_preserve_claims_sequence_subject_and_authority() => ClaimsUnchanged.ShouldBeTrue();
    [Fact] void should_advance_once_per_transition() => _current.Revision.Value.ShouldEqual(2L);
}
