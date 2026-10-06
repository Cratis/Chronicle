// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter.when_round_tripping_a_root_identifier;

public class and_the_schema_uses_upper_case_id : given.an_expando_object_converter
{
    ExpandoObject _source;
    BsonDocument _stored;
    IDictionary<string, object?> _result;

    void Establish()
    {
        schema = JsonSchema.FromJson("""
            { "type": "object", "properties": { "ID": { "type": "string" }, "Name": { "type": "string" } } }
            """);
        _source = new ExpandoObject();
        var properties = (IDictionary<string, object?>)_source;
        properties["ID"] = "the-key";
        properties["Name"] = "Ada";
    }

    void Because()
    {
        _stored = converter.ToBsonDocument(_source, schema);
        _result = converter.ToExpandoObject(_stored, schema);
    }

    [Fact] void should_store_the_identifier_as_mongo_id() => _stored["_id"].AsString.ShouldEqual("the-key");
    [Fact] void should_restore_the_schemas_identifier_property() => _result.ContainsKey("ID").ShouldBeTrue();
    [Fact] void should_not_leak_the_storage_identifier_name() => _result.ContainsKey("_id").ShouldBeFalse();
    [Fact] void should_preserve_the_other_property() => _result["Name"].ShouldEqual("Ada");
}
