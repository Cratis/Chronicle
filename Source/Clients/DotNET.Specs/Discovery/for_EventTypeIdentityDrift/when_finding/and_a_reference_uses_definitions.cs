// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding;

public class and_a_reference_uses_definitions : given.a_renamed_event_type
{
    void Establish()
    {
        _schema = """{"type":"object","properties":{"name":{"$ref":"#/definitions/Name"},"age":{"type":"number"}},"definitions":{"Name":{"type":"string"}}}""";
    }

    void Because() => _result = EventTypeIdentityDrift.Find(_stored, _current, _ => _schema).ToArray();

    [Fact] void should_report_only_unambiguous_shape_matches() => _result.ShouldContainOnly(new ArtifactIdentityDrift(ArtifactKind.EventType, "Original", "Renamed"));
}
