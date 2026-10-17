// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.when_normalizing;

public class and_property_on_root_and_child : given.a_legacy_definition
{
    void Establish() => _schema = JsonSchema.FromJson("""
        {"type":"object","properties":{
          "source":{"type":"string"},
          "lastValue":{"type":"string"},
          "children":{"type":"array","items":{"type":"object","properties":{
            "id":{"type":"string"},"lastValue":{"type":"string"}
          }}}
        }}
        """);

    void Because() => _result = LegacyChildEveryEventMappings.Normalize(_definition, _schema);

    [Fact] void should_keep_the_ambiguous_mapping_at_root() => _result.FromEvery.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_not_guess_a_child_mapping() => _result.Children["children"].All.Properties.ShouldBeEmpty();
    [Fact] void should_preserve_parent_fields_at_root() => _result.FromEvery.Properties["source"].ShouldEqual("$eventContext(eventSourceId)");
}
