// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_caching;

public class the_format : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": { "id": { "type": "string", "format": "guid" } }
    }
    """;

    JsonSchemaProperty _property;
    string? _first;
    string? _second;
    string? _afterChange;
    string? _afterRemoval;

    void Establish() => _property = JsonSchema.FromJson(Json).ActualProperties["id"];

    void Because()
    {
        _first = _property.Format;
        _second = _property.Format;
        _property.Format = "date-time";
        _afterChange = _property.Format;
        _property.Format = null;
        _afterRemoval = _property.Format;
    }

    [Fact] void should_read_the_format() => _first.ShouldEqual("guid");
    [Fact] void should_return_the_same_string_on_repeated_access() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_see_a_format_that_is_set_later() => _afterChange.ShouldEqual("date-time");
    [Fact] void should_see_a_format_that_is_removed_later() => _afterRemoval.ShouldBeNull();
}
