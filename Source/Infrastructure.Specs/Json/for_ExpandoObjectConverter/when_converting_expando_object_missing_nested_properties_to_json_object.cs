// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_expando_object_missing_nested_properties_to_json_object : given.an_expando_object_converter_with_nested_children_schema
{
    ExpandoObject _source;
    JsonObject _result;

    void Establish()
    {
        dynamic parent = new ExpandoObject();
        parent.id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        parent.name = "Parent Name";

        _source = parent;
    }

    void Because() => _result = converter.ToJsonObject(_source, schema);

    [Fact] void should_set_parent_id() => _result["id"]!.GetValue<Guid>().ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_set_parent_name() => _result["name"]!.GetValue<string>().ShouldEqual("Parent Name");
    [Fact] void should_not_add_configurations_property_when_missing_from_source() => _result.ContainsKey("configurations").ShouldBeFalse();
    [Fact] void should_not_have_hubs_property_from_nested_child_schema() => _result.ContainsKey("hubs").ShouldBeFalse();
    [Fact] void should_not_have_configurationId_from_child_schema() => _result.ContainsKey("configurationId").ShouldBeFalse();
    [Fact] void should_not_have_distance_from_child_schema() => _result.ContainsKey("distance").ShouldBeFalse();
    [Fact] void should_not_have_hubId_from_grandchild_schema() => _result.ContainsKey("hubId").ShouldBeFalse();
}
