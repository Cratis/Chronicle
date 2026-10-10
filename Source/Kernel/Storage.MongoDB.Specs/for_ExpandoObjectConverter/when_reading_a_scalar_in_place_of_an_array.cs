// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_reading_a_scalar_in_place_of_an_array : Specification
{
    static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    string _transportJson;
    Exception _error;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","properties":{"involvedUsers":{"type":"array","items":{"type":"string"}}}}
            """);
        var formats = new TypeFormats();
        var stored = new BsonDocument("involvedUsers", "a-lost-array-element");
        var current = new ExpandoObjectConverter(formats).ToExpandoObject(stored, schema);
        _transportJson = new Cratis.Chronicle.Json.ExpandoObjectConverter(formats).ToJsonObject(current, schema).ToJsonString();
    }

    void Because() => _error = Catch.Exception(() => JsonSerializer.Deserialize<ArrayState>(_transportJson, _options));

    [Fact] void should_reject_corrupt_prior_state_rather_than_invent_an_array() => _error.ShouldBeOfExactType<JsonException>();
    [Fact] void should_identify_the_corrupt_field() => ((JsonException)_error).Path.ShouldEqual("$.involvedUsers");

    sealed record ArrayState(IEnumerable<string> InvolvedUsers);
}
