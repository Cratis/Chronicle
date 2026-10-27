// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_the_artifacts_arrive_in_a_different_order : given.a_complete_set_of_definitions
{
    ArtifactFingerprint _original;
    ArtifactFingerprint _reordered;

    void Because()
    {
        _original = ArtifactFingerprintCalculator.Calculate(Build());
        _reordered = ArtifactFingerprintCalculator.Calculate(Reordered());
    }

    [Fact]
    void should_ignore_item_and_dictionary_insertion_order() => _original.ShouldEqual(_reordered);
}
