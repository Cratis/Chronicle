// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.when_normalizing;

public class and_property_is_inherited_by_root : given.a_legacy_definition
{
    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "type": "object",
          "$defs": { "Base": { "type": "object", "properties": { "lastValue": { "type": "string" } } } },
          "allOf": [ { "$ref": "#/$defs/Base" } ],
          "properties": {
            "source": { "type": "string" },
            "children": { "type": "array", "items": { "type": "object", "properties": {
              "id": { "type": "string" }, "lastValue": { "type": "string" }
            } } }
          }
        }
        """);

    void Because() => _result = LegacyChildEveryEventMappings.Normalize(_definition, _schema);

    [Fact] void should_keep_the_inherited_mapping_at_root() => _result.FromEvery.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_not_move_it_to_the_child() => _result.Children["children"].All.Properties.ShouldBeEmpty();
}
