// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_structure_with_nested_children_to_expando_object : given.an_expando_object_converter_with_nested_children_schema
{
    BsonDocument _source;
    dynamic _result;

    void Establish()
    {
        var hub1 = new BsonDocument
        {
            { "hubId", new BsonBinaryData(Guid.Parse("33333333-3333-3333-3333-333333333333"), GuidRepresentation.Standard) },
            { "name", "Hub 1" }
        };

        var hub2 = new BsonDocument
        {
            { "hubId", new BsonBinaryData(Guid.Parse("44444444-4444-4444-4444-444444444444"), GuidRepresentation.Standard) },
            { "name", "Hub 2" }
        };

        var config1 = new BsonDocument
        {
            { "configurationId", new BsonBinaryData(Guid.Parse("22222222-2222-2222-2222-222222222222"), GuidRepresentation.Standard) },
            { "name", "Config 1" },
            { "distance", 100.5 },
            { "hubs", new BsonArray { hub1, hub2 } }
        };

        var config2 = new BsonDocument
        {
            { "configurationId", new BsonBinaryData(Guid.Parse("55555555-5555-5555-5555-555555555555"), GuidRepresentation.Standard) },
            { "name", "Config 2" },
            { "distance", 200.5 },
            { "hubs", new BsonArray() }
        };

        _source = new BsonDocument
        {
            { "_id", new BsonBinaryData(Guid.Parse("11111111-1111-1111-1111-111111111111"), GuidRepresentation.Standard) },
            { "name", "Parent Name" },
            { "configurations", new BsonArray { config1, config2 } }
        };
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_set_parent_id() => ((Guid)_result.id).ShouldEqual(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    [Fact] void should_set_parent_name() => ((string)_result.name).ShouldEqual("Parent Name");
    [Fact] void should_have_configurations_collection() => ((object)_result.configurations).ShouldNotBeNull();
    [Fact] void should_have_two_configurations() => ((IEnumerable<object>)_result.configurations).Count().ShouldEqual(2);
    [Fact] void should_not_have_hubs_property_on_root_object() => ((IDictionary<string, object>)_result).ContainsKey("hubs").ShouldBeFalse();
    [Fact] void should_have_first_configuration_with_correct_id() => ((Guid)_result.configurations[0].configurationId).ShouldEqual(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    [Fact] void should_have_first_configuration_with_correct_name() => ((string)_result.configurations[0].name).ShouldEqual("Config 1");
    [Fact] void should_have_first_configuration_with_correct_distance() => ((double)_result.configurations[0].distance).ShouldEqual(100.5);
    [Fact] void should_have_first_configuration_with_hubs_collection() => ((object)_result.configurations[0].hubs).ShouldNotBeNull();
    [Fact] void should_have_first_configuration_with_two_hubs() => ((IEnumerable<object>)_result.configurations[0].hubs).Count().ShouldEqual(2);
    [Fact] void should_have_first_hub_with_correct_id() => ((Guid)_result.configurations[0].hubs[0].hubId).ShouldEqual(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    [Fact] void should_have_first_hub_with_correct_name() => ((string)_result.configurations[0].hubs[0].name).ShouldEqual("Hub 1");
    [Fact] void should_have_second_hub_with_correct_id() => ((Guid)_result.configurations[0].hubs[1].hubId).ShouldEqual(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    [Fact] void should_have_second_hub_with_correct_name() => ((string)_result.configurations[0].hubs[1].name).ShouldEqual("Hub 2");
    [Fact] void should_have_second_configuration_with_empty_hubs_collection() => ((IEnumerable<object>)_result.configurations[1].hubs).Count().ShouldEqual(0);
}
