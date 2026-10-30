// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationOutcome;

public class when_comparing_outcomes_with_fingerprints : Specification
{
    RegistrationOutcome _first;
    RegistrationOutcome _equal;
    RegistrationOutcome _different;

    void Establish()
    {
        _first = new(true, []) { Fingerprint = new("first") };
        _equal = new(true, []) { Fingerprint = new("first") };
        _different = new(true, []) { Fingerprint = new("different") };
    }

    [Fact]
    void should_compare_equal_fingerprints_by_value() => _first.ShouldEqual(_equal);

    [Fact]
    void should_hash_equal_fingerprints_equally() => _first.GetHashCode().ShouldEqual(_equal.GetHashCode());

    [Fact]
    void should_distinguish_different_fingerprints() => _first.Equals(_different).ShouldBeFalse();

    [Fact]
    void should_default_to_not_set() => new RegistrationOutcome(true, []).Fingerprint.ShouldEqual(ArtifactFingerprint.NotSet);
}
