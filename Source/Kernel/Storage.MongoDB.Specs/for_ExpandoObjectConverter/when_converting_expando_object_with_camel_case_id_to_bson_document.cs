// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_expando_object_with_camel_case_id_to_bson_document : given.an_expando_object_converter
{
    ExpandoObject _source;
    Guid _id;
    BsonDocument _result;

    void Establish()
    {
        _id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        dynamic expando = new ExpandoObject();
        expando.id = _id;
        _source = expando;
    }

    void Because() => _result = converter.ToBsonDocument(_source, schema);

    [Fact] void should_map_id_to_mongodb_id() => _result.GetElement("_id").Value.AsGuid.ShouldEqual(_id);
    [Fact] void should_not_include_camel_case_id_property() => _result.Contains("id").ShouldBeFalse();
}
