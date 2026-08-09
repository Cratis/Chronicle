// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_round_tripping_compliant_enum_ciphertext : Specification
{
    const string Ciphertext = "Mv6b9CejjH6U05D7Nzig0gsc1VFd2QvfoOf7axEys4SMGWCb46YYMslJarlr40g==";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
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
                    "status": {
                        "type": "integer",
                        "enum": [0, 1],
                        "x-enumNames": ["Unknown", "Verified"],
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                }
            }
            """);

        dynamic input = new ExpandoObject();
        input.status = Ciphertext;
        _document = _converter.ToBsonDocument(input, _schema);
    }

    void Because() => _roundTripped = _converter.ToExpandoObject(_document, _schema);

    [Fact] void should_store_the_ciphertext_as_a_bson_string() => _document["status"].IsString.ShouldBeTrue();
    [Fact] void should_keep_the_ciphertext_unchanged() => _document["status"].AsString.ShouldEqual(Ciphertext);
    [Fact] void should_restore_the_opaque_ciphertext() => ((IDictionary<string, object?>)_roundTripped)["status"].ShouldEqual(Ciphertext);
}
