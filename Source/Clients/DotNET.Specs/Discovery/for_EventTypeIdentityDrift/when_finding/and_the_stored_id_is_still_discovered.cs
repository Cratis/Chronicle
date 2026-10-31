// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding;

public class and_the_stored_id_is_still_discovered : given.a_renamed_event_type
{
    void Establish()
    {
        _current = _current with { Artifacts = _current.Artifacts.Add(new(ArtifactKind.EventType, "Original", null)) };
    }

    void Because() => _result = EventTypeIdentityDrift.Find(_stored, _current, _ => _schema).ToArray();

    [Fact] void should_report_only_unambiguous_shape_matches() => _result.ShouldBeEmpty();
}
