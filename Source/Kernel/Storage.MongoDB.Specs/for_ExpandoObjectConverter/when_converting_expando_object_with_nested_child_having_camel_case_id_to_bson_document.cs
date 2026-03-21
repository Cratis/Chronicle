// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_expando_object_with_nested_child_having_camel_case_id_to_bson_document : given.an_expando_object_converter_with_children_having_id_camel_case_schema
{
    ExpandoObject _source;
    Guid _parentId;
    Guid _childId;
    BsonDocument _result;

    void Establish()
    {
        _parentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _childId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        dynamic child = new ExpandoObject();
        child.id = _childId;
        child.name = "Child Name";

        dynamic parent = new ExpandoObject();
        parent.id = _parentId;
        parent.items = new[] { child };

        _source = parent;
    }

    void Because() => _result = converter.ToBsonDocument(_source, schema);

    [Fact] void should_map_parent_id_to_mongodb_id() => _result.GetElement("_id").Value.AsGuid.ShouldEqual(_parentId);
    [Fact] void should_not_include_parent_camel_case_id_property() => _result.Contains("id").ShouldBeFalse();
    [Fact] void should_have_items_array() => _result.GetElement("items").Value.ShouldBeOfExactType<BsonArray>();
    [Fact] void should_map_child_id_to_mongodb_id() => _result.GetElement("items").Value.AsBsonArray[0].AsBsonDocument.GetElement("_id").Value.AsGuid.ShouldEqual(_childId);
    [Fact] void should_not_include_child_camel_case_id_property() => _result.GetElement("items").Value.AsBsonArray[0].AsBsonDocument.Contains("id").ShouldBeFalse();
    [Fact] void should_preserve_child_name() => _result.GetElement("items").Value.AsBsonArray[0].AsBsonDocument.GetElement("name").Value.AsString.ShouldEqual("Child Name");
}
