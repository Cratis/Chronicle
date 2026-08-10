// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_compliant_enum_values : Specification
{
    const string Ciphertext = "Mv6b9CejjH6U05D7Nzig0gsc1VFd2QvfoOf7axEys4SMGWCb46YYMslJarlr40g==";

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _encrypted;
    JsonObject _stored;
    ExpandoObject _released;
    ExpandoObject _shredded;

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
                    },
                    "nullableStatus": {
                        "type": ["integer", "null"],
                        "enum": [0, 1],
                        "x-enumNames": ["Unknown", "Verified"],
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                }
            }
            """);

        _encrypted = _converter.ToExpandoObject(
            new JsonObject
            {
                ["status"] = Ciphertext,
                ["nullableStatus"] = Ciphertext
            },
            _schema);
    }

    void Because()
    {
        _stored = _converter.ToJsonObject(_encrypted, _schema);
        _released = _converter.ToExpandoObject(
            new JsonObject
            {
                ["status"] = "0",
                ["nullableStatus"] = "1"
            },
            _schema);
        _shredded = _converter.ToExpandoObject(new JsonObject { ["status"] = string.Empty }, _schema);
    }

    [Fact] void should_keep_the_non_nullable_ciphertext_opaque() => ((IDictionary<string, object?>)_encrypted)["status"].ShouldEqual(Ciphertext);
    [Fact] void should_keep_the_nullable_ciphertext_opaque() => ((IDictionary<string, object?>)_encrypted)["nullableStatus"].ShouldEqual(Ciphertext);
    [Fact] void should_serialize_the_non_nullable_ciphertext_without_coercion() => _stored["status"]!.GetValue<string>().ShouldEqual(Ciphertext);
    [Fact] void should_serialize_the_nullable_ciphertext_without_coercion() => _stored["nullableStatus"]!.GetValue<string>().ShouldEqual(Ciphertext);
    [Fact] void should_restore_the_zero_enum_value() => ((IDictionary<string, object?>)_released)["status"].ShouldEqual(0);
    [Fact] void should_restore_the_nonzero_nullable_enum_value() => ((IDictionary<string, object?>)_released)["nullableStatus"].ShouldEqual(1);
    [Fact] void should_preserve_the_crypto_shredded_empty_value() => ((IDictionary<string, object?>)_shredded)["status"].ShouldEqual(string.Empty);
}
