// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_nothing_is_registered : given.a_complete_set_of_definitions
{
    ArtifactFingerprint _fingerprint;

    void Because() => _fingerprint = ArtifactFingerprintCalculator.Calculate(ArtifactDefinitionPayloads.Empty);

    [Fact]
    void should_have_a_sha256_fingerprint() => _fingerprint.Value.Length.ShouldEqual(64);

    [Fact]
    void should_be_stable() => _fingerprint.ShouldEqual(ArtifactFingerprintCalculator.Calculate(ArtifactDefinitionPayloads.Empty));

    [Fact]
    void should_differ_from_a_full_set() => _fingerprint.Equals(ArtifactFingerprintCalculator.Calculate(Build())).ShouldBeFalse();
}
