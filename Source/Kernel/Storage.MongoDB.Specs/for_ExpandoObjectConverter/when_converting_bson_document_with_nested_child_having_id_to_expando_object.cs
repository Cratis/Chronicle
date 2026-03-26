// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_bson_document_with_nested_child_having_id_to_expando_object : given.an_expando_object_converter_with_children_having_id_camel_case_schema
{
    BsonDocument _source;
    Guid _parentId;
    Guid _childId;
    dynamic _result;

    void Establish()
    {
        _parentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _childId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var child = new BsonDocument
        {
            { "_id", new BsonBinaryData(_childId, GuidRepresentation.Standard) },
            { "name", "Child Name" }
        };

        _source = new BsonDocument
        {
            { "_id", new BsonBinaryData(_parentId, GuidRepresentation.Standard) },
            { "items", new BsonArray { child } }
        };
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_map_parent_mongo_id_to_id_property() => ((Guid)_result.id).ShouldEqual(_parentId);
    [Fact] void should_not_include_parent_mongo_id_property() => ((IDictionary<string, object>)_result).ContainsKey("_id").ShouldBeFalse();
    [Fact] void should_have_items_collection() => ((object)_result.items).ShouldNotBeNull();
    [Fact] void should_map_child_mongo_id_to_id_property() => ((Guid)_result.items[0].id).ShouldEqual(_childId);
    [Fact] void should_not_include_child_mongo_id_property() => ((IDictionary<string, object>)_result.items[0]).ContainsKey("_id").ShouldBeFalse();
    [Fact] void should_preserve_child_name() => ((string)_result.items[0].name).ShouldEqual("Child Name");
}
