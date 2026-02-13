// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_bson_document_with_id_and_Id_properties_to_expando_object : given.an_expando_object_converter_with_pascal_case_schema
{
    BsonDocument _source;
    Guid _camelCaseId;
    Guid _pascalCaseId;
    dynamic _result;

    void Establish()
    {
        _camelCaseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        _pascalCaseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _source = new BsonDocument
        {
            { "_id", new BsonBinaryData(_camelCaseId, GuidRepresentation.Standard) },
            { "Id", new BsonBinaryData(_pascalCaseId, GuidRepresentation.Standard) }
        };
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_have_camel_case_id_property() => ((IDictionary<string, object>)_result).ContainsKey("id").ShouldBeTrue();
    [Fact] void should_have_pascal_case_id_property() => ((IDictionary<string, object>)_result).ContainsKey("Id").ShouldBeTrue();
    [Fact] void should_set_camel_case_id_from_document_id() => ((Guid)_result.id).ShouldEqual(_camelCaseId);
    [Fact] void should_set_pascal_case_id_from_document_id() => ((Guid)_result.Id).ShouldEqual(_pascalCaseId);
}
