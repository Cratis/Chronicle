// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_case_sensitive_dictionary_values : Specification
{
    const string Content = """{"payload":{"Name":"upper","name":"lower","missing":null,"nested":{"Value":null},"items":[null,{"Name":"a","name":"b"}]}}""";
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    IDictionary<string, object?> _converted;
    JsonObject _roundTripped;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""{"type":"object","properties":{"payload":{"type":"object","additionalProperties":{}}}}""");
    }

    void Because()
    {
        var converted = _converter.ToExpandoObject(JsonNode.Parse(Content)!.AsObject(), _schema);
        _converted = converted;
        _roundTripped = _converter.ToJsonObject(converted, _schema);
    }

    [Fact] void should_keep_string_keys_as_strings() => _converted["payload"].ShouldBeOfExactType<Dictionary<string, object>>();
    [Fact] void should_preserve_case_sensitive_keys_and_nulls() => _roundTripped.ToJsonString().ShouldEqual(Content);
    [Fact] void should_preserve_the_null_dictionary_entry() => ((Dictionary<string, object>)_converted["payload"]!)["missing"].ShouldBeNull();
}
