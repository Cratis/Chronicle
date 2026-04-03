// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_bson_document_with_nested_child_having_pascal_case_id_to_expando_object : given.an_expando_object_converter_with_children_having_id_pascal_case_schema
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
            { "Name", "Child Name" }
        };

        _source = new BsonDocument
        {
            { "_id", new BsonBinaryData(_parentId, GuidRepresentation.Standard) },
            { "Items", new BsonArray { child } }
        };
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_map_parent_mongo_id_to_pascal_case_id_property() => ((Guid)_result.Id).ShouldEqual(_parentId);
    [Fact] void should_not_include_parent_camel_case_id_property() => ((IDictionary<string, object>)_result).ContainsKey("id").ShouldBeFalse();
    [Fact] void should_not_include_parent_mongo_id_property() => ((IDictionary<string, object>)_result).ContainsKey("_id").ShouldBeFalse();
    [Fact] void should_have_items_collection() => ((object)_result.Items).ShouldNotBeNull();
    [Fact] void should_map_child_mongo_id_to_pascal_case_id_property() => ((Guid)_result.Items[0].Id).ShouldEqual(_childId);
    [Fact] void should_not_include_child_camel_case_id_property() => ((IDictionary<string, object>)_result.Items[0]).ContainsKey("id").ShouldBeFalse();
    [Fact] void should_not_include_child_mongo_id_property() => ((IDictionary<string, object>)_result.Items[0]).ContainsKey("_id").ShouldBeFalse();
    [Fact] void should_preserve_child_name() => ((string)_result.Items[0].Name).ShouldEqual("Child Name");
}
