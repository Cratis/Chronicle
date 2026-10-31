// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_a_projection_definition_changes : given.a_complete_set_of_definitions
{
    ArtifactFingerprint _original;
    ArtifactFingerprint _changed;

    void Because()
    {
        var definitions = Build();
        _original = ArtifactFingerprintCalculator.Calculate(definitions);
        definitions.Projections.First().From.First().Value.Properties["first"] = "event.changed";
        _changed = ArtifactFingerprintCalculator.Calculate(definitions);
    }

    [Fact]
    void should_change_the_fingerprint() => _original.Equals(_changed).ShouldBeFalse();
}
