// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_phase_is_stale : given.an_authority_refusal
{
    async Task Establish() => await Transition(Run(1));
    async Task Because() => await Refuse();
    [Fact] void should_refuse_all_six_live_mutations_during_a_run() => AllRefused.ShouldBeTrue();
    [Fact] void should_preserve_claims_sequence_subject_and_authority() => ClaimsUnchanged.ShouldBeTrue();
}
