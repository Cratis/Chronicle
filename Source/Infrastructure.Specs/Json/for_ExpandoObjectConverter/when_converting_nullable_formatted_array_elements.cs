// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_converting_nullable_formatted_array_elements : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _result;
    JsonObject _roundTripped;

    async Task Establish()
    {
        _converter = new(new TypeFormats());
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"numbers":{"type":"array","items":{"type":["integer","null"],"format":"int32?"}}}}""");
    }

    void Because()
    {
        _result = _converter.ToExpandoObject(JsonNode.Parse("""{"numbers":[1,null,3]}""")!.AsObject(), _schema);
        _roundTripped = _converter.ToJsonObject(_result, _schema);
    }

    [Fact] void should_preserve_null_positions() => ((object?[])((IDictionary<string, object?>)_result)["numbers"]!).ShouldContainOnly(1, null, 3);
    [Fact] void should_round_trip_null_positions() => JsonNode.DeepEquals(_roundTripped, JsonNode.Parse("""{"numbers":[1,null,3]}""")).ShouldBeTrue();
}
