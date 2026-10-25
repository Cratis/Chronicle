// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_two_sets_are_built_independently : given.a_complete_set_of_definitions
{
    ArtifactFingerprint _first;
    ArtifactFingerprint _second;

    void Because()
    {
        _first = ArtifactFingerprintCalculator.Calculate(Build());
        _second = ArtifactFingerprintCalculator.Calculate(Build());
    }

    [Fact]
    void should_have_equal_fingerprints() => _first.ShouldEqual(_second);

    [Fact]
    void should_have_a_sha256_length() => _first.Value.Length.ShouldEqual(64);

    [Fact]
    void should_have_only_lowercase_hex_characters() => _first.Value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f').ShouldBeTrue();
}
