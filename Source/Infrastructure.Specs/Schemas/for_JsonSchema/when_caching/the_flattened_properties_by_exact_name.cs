// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_flattened_properties_by_exact_name : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": { "Name": { "type": "string" }, "name": { "type": "integer" } }
    }
    """;

    JsonSchema _schema;
    IReadOnlyDictionary<string, JsonSchemaProperty> _first;
    IReadOnlyDictionary<string, JsonSchemaProperty> _second;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because()
    {
        _first = _schema.GetFlattenedPropertiesByExactName();
        _second = _schema.GetFlattenedPropertiesByExactName();
    }

    [Fact] void should_return_the_same_instance_on_repeated_access() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_hold_every_property() => _first.Count.ShouldEqual(2);
    [Fact] void should_tell_names_apart_by_case() => _first["name"].Type.ShouldEqual(JsonObjectType.Integer);
}
