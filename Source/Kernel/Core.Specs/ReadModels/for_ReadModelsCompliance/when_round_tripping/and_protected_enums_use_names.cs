// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping;

public class and_protected_enums_use_names : Specification
{
    JsonSchema _schema;
    JsonSchemaMetadataManager _manager;
    ReadModelsCompliance _compliance;
    ExpandoObject _encrypted;
    ExpandoObject _released;
    JsonObject _legacyReleased;

    void Establish()
    {
        _schema = JsonSchema.FromJson(
            """
            {
              "type": "object",
              "properties": {
                "status": {
                  "type": "integer", "enum": [0, 2], "x-enumNames": ["Unknown", "Verified"],
                  "compliance": [{ "metadataType": "PII", "details": "" }]
                }
              }
            }
            """);
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new(_manager, new ExpandoObjectConverter(new TypeFormats()));
    }

    async Task Because()
    {
        dynamic input = new ExpandoObject();
        input.status = status.Verified;
        _encrypted = await _compliance.Apply("test-store", "test-namespace", _schema, "owner", input);
        _released = await _compliance.Release("test-store", "test-namespace", _schema, _encrypted);
        var legacy = await _manager.Apply("test-store", "test-namespace", _schema, "owner", new JsonObject { ["status"] = 2 });
        _legacyReleased = await _manager.Release("test-store", "test-namespace", _schema, "owner", legacy);
    }

    [Fact] void should_protect_the_named_enum() => ((IDictionary<string, object?>)_encrypted)["status"].ShouldNotEqual("Verified");
    [Fact] void should_restore_the_nonzero_enum_value() => ((IDictionary<string, object?>)_released)["status"].ShouldEqual(2);
    [Fact] void should_preserve_legacy_numeric_enum_values() => _legacyReleased["status"]!.GetValue<int>().ShouldEqual(2);

    enum status
    {
        Unknown = 0,
        Verified = 2
    }
}
