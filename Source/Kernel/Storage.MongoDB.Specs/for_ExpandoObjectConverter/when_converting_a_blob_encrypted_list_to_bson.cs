// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_a_blob_encrypted_list_to_bson : Specification
{
    const string Blob = "Mv6b9CejjH6U05D7Nzig0gsc1VFd2QvfoOf7axEys4SMGWCb46YYMslJarlr40g==";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _input;
    BsonDocument _document;
    ExpandoObject _roundTripped;

    void Establish()
    {
        _converter = new(new TypeFormats());
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

        // A coarse [PII] list is blob-encrypted to a single ciphertext string even though the schema type
        // stays array. The sink must persist it as that scalar string, not shred it into per-character bson.
        _input = new ExpandoObject();
        ((IDictionary<string, object?>)_input)["scores"] = Blob;
    }

    void Because()
    {
        _document = _converter.ToBsonDocument(_input, _schema);
        _roundTripped = _converter.ToExpandoObject(_document, _schema);
    }

    [Fact] void should_store_the_blob_as_a_bson_string() => _document["scores"].IsString.ShouldBeTrue();

    [Fact] void should_not_shred_the_blob_into_an_array() => _document["scores"].IsBsonArray.ShouldBeFalse();

    [Fact] void should_round_trip_the_blob_back_to_the_string() => ((IDictionary<string, object?>)_roundTripped)["scores"].ShouldEqual(Blob);
}
