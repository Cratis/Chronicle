// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_round_tripping_protected_scalars;

public class and_the_schema_declares_non_string_kinds : Specification
{
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _encrypted;
    JsonObject _released;
    JsonObject _erased;
    InMemoryEncryptionKeyStorage _keys;

    async Task Establish()
    {
        var keys = _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, keys, encryption),
                new EncryptedSubjectValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "address": {
                  "type": "object",
                  "properties": {
                    "number": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "fraction": { "type": "number", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "verified": { "type": "boolean", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "nullableNumber": { "type": ["integer", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "text": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "secretNumber": { "type": "integer", "security": [{ "metadataType": "EncryptedSubject", "details": "" }] },
                    "numbers": { "type": "array", "items": { "type": "integer", "compliance": [{ "metadataType": "PII", "details": "" }] } },
                    "optionalNumber": { "type": ["integer", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] }
                  }
                }
              }
            }
            """);
        _encrypted = await _manager.Apply("test-store", "test-namespace", _schema, "scalar-subject", JsonNode.Parse(
            """{"address":{"number":42,"fraction":1.5,"verified":true,"nullableNumber":null,"text":"42","secretNumber":42,"numbers":[42],"optionalNumber":42}}""")!.AsObject());
    }

    async Task Because()
    {
        _released = await _manager.Release("test-store", "test-namespace", _schema, "scalar-subject", _encrypted);
        await _keys.DeleteFor("test-store", "test-namespace", "scalar-subject");
        _erased = await _manager.Release("test-store", "test-namespace", _schema, "scalar-subject", _encrypted);
    }

    [Fact] void should_release_the_integer_as_a_number() => _released["address"]!["number"]!.GetValueKind().ShouldEqual(JsonValueKind.Number);
    [Fact] void should_release_the_fraction_as_a_number() => _released["address"]!["fraction"]!.GetValueKind().ShouldEqual(JsonValueKind.Number);
    [Fact] void should_release_the_boolean_as_a_boolean() => _released["address"]!["verified"]!.GetValueKind().ShouldEqual(JsonValueKind.True);
    [Fact] void should_keep_the_nullable_null_value_null() => (_released["address"]!["nullableNumber"] is null).ShouldBeTrue();
    [Fact] void should_keep_genuine_strings_as_strings() => _released["address"]!["text"]!.GetValueKind().ShouldEqual(JsonValueKind.String);
    [Fact] void should_release_the_confidential_integer_as_a_number() => _released["address"]!["secretNumber"]!.GetValueKind().ShouldEqual(JsonValueKind.Number);
    [Fact] void should_release_integer_array_elements_as_numbers() => _released["address"]!["numbers"]![0]!.GetValue<int>().ShouldEqual(42);
    [Fact] void should_erase_the_integer_to_zero() => _erased["address"]!["number"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_erase_the_number_to_zero() => _erased["address"]!["fraction"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_erase_the_boolean_to_false() => _erased["address"]!["verified"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_erase_the_nullable_number_to_null() => (_erased["address"]!["optionalNumber"] is null).ShouldBeTrue();
    [Fact] void should_erase_integer_array_elements_to_zero() => _erased["address"]!["numbers"]![0]!.GetValue<int>().ShouldEqual(0);
}
