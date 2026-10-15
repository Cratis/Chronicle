// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Discovery;

namespace Cratis.Chronicle.Registrations.for_RegistrationOutcome;

public class when_comparing_outcomes_with_identity_drift : Specification
{
    RegistrationOutcome _outcome;
    RegistrationOutcome _equivalent;

    void Establish()
    {
        _outcome = new(true, []) { IdentityDrift = ImmutableList.Create(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Old", "New")) };
        _equivalent = new(true, []) { IdentityDrift = ImmutableList.Create(new ArtifactIdentityDrift(ArtifactKind.Reactor, "Old", "New")) };
    }

    [Fact] void should_compare_findings_by_value() => _outcome.Equals(_equivalent).ShouldBeTrue();
    [Fact] void should_hash_equivalent_findings_alike() => _outcome.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_distinguish_an_outcome_without_findings() => _outcome.Equals(new RegistrationOutcome(true, [])).ShouldBeFalse();
    [Fact] void should_distinguish_different_findings() => _outcome.Equals(_equivalent with { IdentityDrift = ImmutableList.Create(new ArtifactIdentityDrift(ArtifactKind.EventType, "Old", "New")) }).ShouldBeFalse();
}
