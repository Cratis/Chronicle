// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_structure_with_nested_children_to_bson_document : given.an_expando_object_converter_with_nested_children_schema
{
    ExpandoObject _source;
    dynamic _sourceDynamic;
    BsonDocument _result;

    void Establish()
    {
        dynamic parent = new ExpandoObject();
        parent.id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        parent.name = "Parent Name";

        dynamic config1 = new ExpandoObject();
        config1.configurationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        config1.name = "Config 1";
        config1.distance = 100.5;

        dynamic hub1 = new ExpandoObject();
        hub1.hubId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        hub1.name = "Hub 1";

        dynamic hub2 = new ExpandoObject();
        hub2.hubId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        hub2.name = "Hub 2";

        config1.hubs = new[] { hub1, hub2 };

        dynamic config2 = new ExpandoObject();
        config2.configurationId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        config2.name = "Config 2";
        config2.distance = 200.5;
        config2.hubs = Array.Empty<ExpandoObject>();

        parent.configurations = new[] { config1, config2 };

        _source = parent;
        _sourceDynamic = parent;
    }

    void Because() => _result = converter.ToBsonDocument(_source, schema);

    [Fact] void should_set_parent_id() => _result.GetElement("_id").Value.AsGuid.ShouldEqual((Guid)_sourceDynamic.id);
    [Fact] void should_set_parent_name() => _result.GetElement("name").Value.AsString.ShouldEqual((string)_sourceDynamic.name);
    [Fact] void should_have_configurations_array() => _result.GetElement("configurations").Value.ShouldBeOfExactType<BsonArray>();
    [Fact] void should_have_two_configurations() => _result.GetElement("configurations").Value.AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_not_have_hubs_array_on_root_document() => _result.Contains("hubs").ShouldBeFalse();
    [Fact] void should_have_first_configuration_with_correct_id() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("configurationId").Value.AsGuid.ShouldEqual(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    [Fact] void should_have_first_configuration_with_correct_name() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("name").Value.AsString.ShouldEqual("Config 1");
    [Fact] void should_have_first_configuration_with_correct_distance() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("distance").Value.AsDouble.ShouldEqual(100.5);
    [Fact] void should_have_first_configuration_with_hubs_array() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("hubs").Value.ShouldBeOfExactType<BsonArray>();
    [Fact] void should_have_first_configuration_with_two_hubs() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("hubs").Value.AsBsonArray.Count.ShouldEqual(2);
    [Fact] void should_have_first_hub_with_correct_id() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("hubs").Value.AsBsonArray[0].AsBsonDocument.GetElement("hubId").Value.AsGuid.ShouldEqual(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    [Fact] void should_have_first_hub_with_correct_name() => _result.GetElement("configurations").Value.AsBsonArray[0].AsBsonDocument.GetElement("hubs").Value.AsBsonArray[0].AsBsonDocument.GetElement("name").Value.AsString.ShouldEqual("Hub 1");
    [Fact] void should_have_second_configuration_with_empty_hubs_array() => _result.GetElement("configurations").Value.AsBsonArray[1].AsBsonDocument.GetElement("hubs").Value.AsBsonArray.Count.ShouldEqual(0);
}
