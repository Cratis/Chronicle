// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_getting_flattened_properties_for_nested_children_schema : Specification
{
    JsonSchema _schema;
    IEnumerable<JsonSchemaProperty> _result;

    void Establish()
    {
        _schema = JsonSchema.FromType<ParentWithNestedChildren>();
    }

    void Because() => _result = _schema.GetFlattenedProperties();

    [Fact] void should_include_id_property() => _result.Select(_ => _.Name).ShouldContain("id");
    [Fact] void should_include_name_property() => _result.Select(_ => _.Name).ShouldContain("name");
    [Fact] void should_include_configurations_property() => _result.Select(_ => _.Name).ShouldContain("configurations");
    [Fact] void should_not_include_hubs_property_from_nested_child() => _result.Select(_ => _.Name).ShouldNotContain("hubs");
    [Fact] void should_not_include_configurationId_from_child() => _result.Select(_ => _.Name).ShouldNotContain("configurationId");
    [Fact] void should_not_include_distance_from_child() => _result.Select(_ => _.Name).ShouldNotContain("distance");
    [Fact] void should_not_include_hubId_from_grandchild() => _result.Select(_ => _.Name).ShouldNotContain("hubId");
    [Fact] void should_have_exactly_three_properties() => _result.Count().ShouldEqual(3);
}
