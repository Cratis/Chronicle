// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_LegacyChildEveryEventMappings.when_normalizing;

public class and_property_on_two_children : given.a_legacy_definition
{
    void Establish()
    {
        _definition.Children["others"] = Child();
        _definition.Children["children"].Children["grandchildren"] = Child();
        _definition = _definition with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["detail"] = Child() } };
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "source":{"type":"string"},
              "children":{"type":"array","items":{"type":"object","properties":{
                "lastValue":{"type":"string"},
                "grandchildren":{"type":"array","items":{"type":"object","properties":{"lastValue":{"type":"string"}}}}
              }}},
              "others":{"type":"array","items":{"type":"object","properties":{"lastValue":{"type":"string"}}}},
              "detail":{"type":"object","properties":{"lastValue":{"type":"string"}}}
            }}
            """);
    }

    void Because() => _result = LegacyChildEveryEventMappings.Normalize(_definition, _schema);

    [Fact] void should_map_the_first_child() => _result.Children["children"].All.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_map_the_second_child() => _result.Children["others"].All.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_resolve_the_grandchild_using_its_own_schema() => _result.Children["children"].Children["grandchildren"].All.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_map_the_nested_object() => _result.Nested!["detail"].All.Properties["lastValue"].ShouldEqual("value");
    [Fact] void should_remove_the_mapping_from_root() => _result.FromEvery.Properties.ContainsKey("lastValue").ShouldBeFalse();
    [Fact] void should_keep_parent_fields_out_of_all_descendants() => _result.Children.Values.Concat(_result.Nested!.Values).All(child => !child.All.Properties.ContainsKey("source")).ShouldBeTrue();
}
