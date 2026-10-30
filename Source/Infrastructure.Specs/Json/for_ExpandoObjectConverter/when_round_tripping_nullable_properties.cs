// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_nullable_properties : Specification
{
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    JsonObject _nullDocument;
    JsonObject _valueDocument;
    JsonObject _defaultDocument;
    IDictionary<string, object?> _nullResult;
    IDictionary<string, object?> _absentResult;
    IDictionary<string, object?> _explicitNullResult;
    IDictionary<string, object?> _valueResult;
    IDictionary<string, object?> _defaultResult;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromJson("""
            {"type":"object","properties":{
              "text":{"type":["string","null"]},
              "amount":{"type":["number","null"],"format":"decimal?"},
              "optionalCount":{"type":["integer","null"],"format":"int32?"},
              "count":{"type":"integer","format":"int32"}
            }}
            """);
    }

    void Because()
    {
        (_nullDocument, _nullResult) = RoundTrip(null, null, null);
        (_valueDocument, _valueResult) = RoundTrip("present", 193.58m, 42);
        (_defaultDocument, _defaultResult) = RoundTrip(string.Empty, 0m, 0);
        var absent = _converter.ToExpandoObject(new JsonObject(), _schema);
        var stored = _converter.ToJsonObject(absent, _schema);
        _absentResult = _converter.ToExpandoObject(JsonNode.Parse(stored.ToJsonString())!.AsObject(), _schema);
        _explicitNullResult = _converter.ToExpandoObject(JsonNode.Parse("""{"text":null,"amount":null,"optionalCount":null}""")!.AsObject(), _schema);
    }

    [Fact] void should_omit_nullable_nulls_from_stored_content() => _nullDocument.ContainsKey("amount").ShouldBeFalse();
    [Fact] void should_read_nullable_null_as_null() => ReadValue(_nullResult, "amount").ShouldBeNull();
    [Fact] void should_read_an_absent_nullable_property_as_null() => ReadValue(_absentResult, "amount").ShouldBeNull();
    [Fact] void should_treat_explicit_null_and_absent_equivalently() => _explicitNullResult.ShouldEqual(_absentResult);
    [Fact] void should_read_an_explicit_nullable_null_as_null() => ReadValue(_explicitNullResult, "amount").ShouldBeNull();
    [Fact] void should_preserve_non_null_text() => _valueResult["text"].ShouldEqual("present");
    [Fact] void should_preserve_a_non_null_nullable_decimal() => _valueResult["amount"].ShouldEqual(193.58m);
    [Fact] void should_store_a_non_null_nullable_decimal() => _valueDocument["amount"]!.GetValue<decimal>().ShouldEqual(193.58m);
    [Fact] void should_store_a_nullable_decimal_holding_zero() => _defaultDocument.ContainsKey("amount").ShouldBeTrue();
    [Fact] void should_read_a_nullable_decimal_holding_zero_as_a_value() => _defaultResult["amount"].ShouldEqual(0m);
    [Fact] void should_store_a_nullable_integer_holding_zero() => _defaultDocument.ContainsKey("optionalCount").ShouldBeTrue();
    [Fact] void should_read_a_nullable_integer_holding_zero_as_a_value() => _defaultResult["optionalCount"].ShouldEqual(0);
    [Fact] void should_preserve_an_empty_nullable_string() => _defaultResult["text"].ShouldEqual(string.Empty);
    [Fact] void should_materialize_a_missing_non_nullable_integer_as_its_type_default() => _absentResult["count"].ShouldEqual(0);

    (JsonObject Document, IDictionary<string, object?> Result) RoundTrip(string? text, decimal? amount, int? optionalCount)
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        values["text"] = text;
        values["amount"] = amount;
        values["optionalCount"] = optionalCount;
        var appended = _converter.ToJsonObject(content, _schema);
        var stored = JsonNode.Parse(appended.ToJsonString())!.AsObject();
        return (appended, _converter.ToExpandoObject(stored, _schema));
    }

    static object? ReadValue(IDictionary<string, object?> values, string property) => values.TryGetValue(property, out var value) ? value : null;
}
