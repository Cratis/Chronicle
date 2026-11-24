// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_revision_is_stale : given.an_authority_refusal
{
    async Task Establish() => await Transition(UniqueConstraintWritePhase.Live);
    async Task Because() => await Refuse();
    [Fact] void should_refuse_all_six_mutations_at_the_previous_revision() => AllRefused.ShouldBeTrue();
    [Fact] void should_preserve_claims_sequence_subject_and_authority() => ClaimsUnchanged.ShouldBeTrue();
}
