// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Specs.Compliance.given;

public class protected_values
{
    public static readonly EventStoreName EventStore = "test-store";
    public static readonly EventStoreNamespaceName Namespace = "test-namespace";
    public const string Subject = "some-source";

    readonly JsonSchemaMetadataManager _manager;
    public readonly ExpandoObjectConverter Converter = new(new TypeFormats());
    public readonly JsonSchema Schema;
    public readonly JsonObject Plaintext;

    public protected_values()
    {
        var properties = new JsonObject();
        Plaintext = new JsonObject();
        foreach (var scope in new[] { "Subject", "Namespace", "Global" })
        {
            foreach (var (name, type, value) in new (string Name, string Type, JsonNode Value)[]
            {
                ("Count", "integer", JsonValue.Create(37)),
                ("Flag", "boolean", JsonValue.Create(true)),
                ("Rate", "number", JsonNode.Parse("12.5")!),
                ("Object", "object", new JsonObject { ["count"] = 37 }),
                ("Array", "array", new JsonArray(true, false))
            })
            {
                var property = new JsonObject
                {
                    ["type"] = type,
                    ["security"] = new JsonArray(new JsonObject { ["metadataType"] = $"Encrypted{scope}", ["details"] = string.Empty })
                };
                if (type == "object")
                {
                    property["properties"] = new JsonObject { ["count"] = new JsonObject { ["type"] = "integer" } };
                }
                if (type == "array")
                {
                    property["items"] = new JsonObject { ["type"] = "boolean" };
                }
                properties[$"{scope}{name}"] = property;
                Plaintext[$"{scope}{name}"] = value;
            }
        }
        Schema = JsonSchema.FromJson(new JsonObject { ["type"] = "object", ["properties"] = properties }.ToJsonString());
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new EncryptedSubjectValueHandler(provisioner, keys, encryption),
                new EncryptedNamespaceValueHandler(provisioner, keys, encryption),
                new EncryptedGlobalValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    public async Task<ExpandoObject> Encrypt() => Converter.ToExpandoObject(await _manager.Apply(EventStore, Namespace, Schema, Subject, Plaintext), Schema);

    public async Task<JsonObject> Release(ExpandoObject content)
    {
        var released = await _manager.Release(EventStore, Namespace, Schema, Subject, Converter.ToJsonObject(content, Schema));
        return Converter.ToJsonObject(Converter.ToExpandoObject(released, Schema), Schema);
    }
}
