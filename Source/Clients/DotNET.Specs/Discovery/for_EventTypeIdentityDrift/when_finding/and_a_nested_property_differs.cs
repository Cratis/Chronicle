// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery.for_EventTypeIdentityDrift.when_finding;

public class and_a_nested_property_differs : given.a_renamed_event_type
{
    void Establish()
    {
        _schema = """{"type":"object","properties":{"name":{"type":"object","properties":{"value":{"type":"string"}}},"age":{"type":"number"}}}""";
    }

    void Because() => _result = EventTypeIdentityDrift.Find(_stored, _current, _ => _schema).ToArray();

    [Fact] void should_report_only_unambiguous_shape_matches() => _result.ShouldBeEmpty();
}
