// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_json_object_missing_nested_properties_to_expando_object : given.an_expando_object_converter_with_nested_children_schema
{
    JsonObject _source;
    dynamic _result;

    void Establish()
    {
        _source = new JsonObject
        {
            ["id"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ["name"] = "Parent Name"
        };
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_set_parent_id() => ((Guid)_result.id).ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_set_parent_name() => ((string)_result.name).ShouldEqual("Parent Name");
    [Fact] void should_not_add_configurations_property_when_missing_from_source() => ((IDictionary<string, object>)_result).ContainsKey("configurations").ShouldBeFalse();
    [Fact] void should_not_have_hubs_property_from_nested_child_schema() => ((IDictionary<string, object>)_result).ContainsKey("hubs").ShouldBeFalse();
    [Fact] void should_not_have_configurationId_from_child_schema() => ((IDictionary<string, object>)_result).ContainsKey("configurationId").ShouldBeFalse();
    [Fact] void should_not_have_distance_from_child_schema() => ((IDictionary<string, object>)_result).ContainsKey("distance").ShouldBeFalse();
    [Fact] void should_not_have_hubId_from_grandchild_schema() => ((IDictionary<string, object>)_result).ContainsKey("hubId").ShouldBeFalse();
}
