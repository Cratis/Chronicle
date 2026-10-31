// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding;

public class and_the_schemas_differ_only_by_title : given.a_renamed_event_type
{
    void Because() => _result = EventTypeIdentityDrift.Find(_stored, _current, _ => _schema).ToArray();

    [Fact] void should_report_only_unambiguous_shape_matches() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.EventType, "Original", "Renamed"));
}
