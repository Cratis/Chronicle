// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_reading_the_type_of_a_parsed_schema;

/// <summary>
/// A schema parsed from JSON text holds its type name as a JSON element rather than a string; the type must be read
/// correctly from it, for a single type name as well as for a list of them.
/// </summary>
public class from_json_text : Specification
{
    JsonObjectType _single;
    JsonObjectType _list;
    JsonObjectType _unknown;

    void Because()
    {
        _single = JsonSchema.FromJson("""{"type":"string"}""").Type;
        _list = JsonSchema.FromJson("""{"type":["string","null"]}""").Type;
        _unknown = JsonSchema.FromJson("""{"type":"mystery"}""").Type;
    }

    [Fact] void should_read_a_single_type_name() => _single.ShouldEqual(JsonObjectType.String);
    [Fact] void should_read_a_list_of_type_names() => _list.ShouldEqual(JsonObjectType.String | JsonObjectType.Null);
    [Fact] void should_read_an_unknown_type_name_as_none() => _unknown.ShouldEqual(JsonObjectType.None);
}
