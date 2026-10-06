// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter.when_round_tripping_a_root_identifier;

public class and_multiple_identifier_spellings_include_pascal_case_id : given.an_expando_object_converter
{
    IDictionary<string, object?> _result;

    void Establish() => schema = JsonSchema.FromJson("""
        { "type": "object", "properties": { "ID": { "type": "string" }, "Id": { "type": "string" } } }
        """);

    void Because() => _result = converter.ToExpandoObject(new BsonDocument("_id", "the-key"), schema);

    [Fact] void should_prefer_pascal_case_id() => _result["Id"].ShouldEqual("the-key");
    [Fact] void should_not_use_upper_case_id() => _result.ContainsKey("ID").ShouldBeFalse();
}
