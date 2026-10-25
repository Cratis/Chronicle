// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_ArtifactFingerprintCalculator.when_calculating;

public class and_only_last_updated_and_event_store_differ : given.a_complete_set_of_definitions
{
    ArtifactFingerprint _original;
    ArtifactFingerprint _changed;

    void Because()
    {
        var definitions = Build();
        _original = ArtifactFingerprintCalculator.Calculate(definitions);
        foreach (var eventType in definitions.EventTypes)
        {
            eventType.EventStore = "another-store";
        }

        foreach (var projection in definitions.Projections)
        {
            projection.LastUpdated = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        }

        _changed = ArtifactFingerprintCalculator.Calculate(definitions);
    }

    [Fact]
    void should_ignore_transient_and_store_specific_values() => _original.ShouldEqual(_changed);
}
