// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter.when_round_tripping_a_root_identifier;

public class and_the_schema_uses_mixed_case_id : given.an_expando_object_converter
{
    ExpandoObject _source;
    IDictionary<string, object?> _result;

    void Establish()
    {
        schema = JsonSchema.FromJson("""{ "type": "object", "properties": { "iD": { "type": "string" } } }""");
        _source = new ExpandoObject();
        ((IDictionary<string, object?>)_source)["iD"] = "the-key";
    }

    void Because() => _result = converter.ToExpandoObject(converter.ToBsonDocument(_source, schema), schema);

    [Fact] void should_restore_the_schemas_identifier_spelling() => _result["iD"].ShouldEqual("the-key");
    [Fact] void should_not_leak_the_storage_identifier_name() => _result.ContainsKey("_id").ShouldBeFalse();
}
