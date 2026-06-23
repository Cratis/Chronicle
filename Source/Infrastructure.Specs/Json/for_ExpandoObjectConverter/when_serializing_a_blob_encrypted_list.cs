// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_serializing_a_blob_encrypted_list : Specification
{
    const string Blob = "Mv6b9CejjH6U05D7Nzig0gsc1VFd2QvfoOf7axEys4SMGWCb46YYMslJarlr40g==";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _input;
    JsonObject _json;
    ExpandoObject _roundTripped;

    void Establish()
    {
        _converter = new(new TypeFormats());

        // A coarse [PII] on a whole list is blob-encrypted to a single ciphertext string even though the
        // schema type stays array. The converter must keep it a scalar string in both directions; treating
        // the string as an enumerable of characters shreds the ciphertext and breaks the release round trip.
        _schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "scores": {
                        "type": "array",
                        "items": { "type": "object", "properties": { "value": { "type": "integer" } } }
                    }
                }
            }
            """);

        _input = new ExpandoObject();
        ((IDictionary<string, object?>)_input)["scores"] = Blob;
    }

    void Because()
    {
        _json = _converter.ToJsonObject(_input, _schema);
        _roundTripped = _converter.ToExpandoObject(_json, _schema);
    }

    [Fact] void should_serialize_the_blob_as_a_scalar_string() => _json["scores"]!.GetValue<string>().ShouldEqual(Blob);

    [Fact] void should_not_shred_the_blob_into_an_array() => (_json["scores"] is JsonArray).ShouldBeFalse();

    [Fact] void should_round_trip_the_blob_back_to_the_string() => ((IDictionary<string, object?>)_roundTripped)["scores"].ShouldEqual(Blob);
}
