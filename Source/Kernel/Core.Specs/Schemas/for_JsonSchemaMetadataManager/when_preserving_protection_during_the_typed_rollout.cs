// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_preserving_protection_during_the_typed_rollout : Specification
{
    [Theory]
    [InlineData(195, "PII")]
    [InlineData(196, "PII")]
    [InlineData(195, "EncryptedGlobal")]
    [InlineData(196, "EncryptedGlobal")]
    async Task should_preserve_the_ordinary_reference_handler(int minor, string type)
    {
        var (manager, _, _) = Create(minor);
        var node = JsonNode.Parse("""
            {"type":"object","$defs":{"personal":{"type":"string"}},"properties":{
              "value":{"$ref":"#/$defs/personal","$dynamicRef":"https://unavailable.example/plain"}
            }}
            """);
        var category = type == "PII" ? "compliance" : "security";
        node["$defs"]["personal"][category] = new JsonArray(new JsonObject { ["metadataType"] = type, ["details"] = string.Empty });
        var schema = new JsonSchema(node.AsObject());
        var plain = new JsonObject { ["value"] = "secret" };
        var stored = await manager.Apply("store", "tenant", schema, "subject", plain);
        stored["value"].GetValue<string>().ShouldNotEqual("secret");
        Version(stored).ShouldEqual(minor == 195 ? 3 : 4);
        await ShouldRoundTrip(manager, schema, stored, plain);
    }

    [Fact]
    async Task should_keep_own_and_ordinary_handlers_together()
    {
        var (manager, _, _) = Create(195);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","$defs":{"secret":{"type":"string","security":[{"metadataType":"EncryptedGlobal","details":""}]}},
             "properties":{"value":{"$ref":"#/$defs/secret","$dynamicRef":"https://unavailable.example/plain",
               "compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        var types = manager.MetadataFor(schema.Properties["value"], forComparison: true).Select(entry => entry.Metadata.metadataType).ToArray();
        types.ShouldContain("PII");
        types.ShouldContain("EncryptedGlobal");
    }

    [Fact]
    async Task should_preserve_skipped_plaintext_and_warn_before_the_floor()
    {
        var (manager, _, logger) = Create(195);
        var schema = await ContextualSchema(false);
        var plain = new JsonObject { ["value"] = 42 };
        var stored = await manager.Apply("store", "tenant", schema, "subject", plain);
        JsonNode.DeepEquals(stored, plain).ShouldBeTrue();
        await ShouldRoundTrip(manager, schema, stored, plain);
        var status = await manager.ReleaseWithStatus("store", "tenant", schema, "subject", stored);
        status.Statuses["value"].ShouldEqual(ProtectedValueReleaseStatus.Plain);
        logger.Warnings.Exists(fields =>
            fields.Any(field => field.Key.Equals("Property", StringComparison.OrdinalIgnoreCase) && Equals(field.Value, "value")) &&
            fields.Any(field => field.Key.Equals("Floor", StringComparison.OrdinalIgnoreCase) && Equals(field.Value, "19.196.0"))).ShouldBeTrue();
    }

    [Theory]
    [InlineData(195, true, 3)]
    [InlineData(196, true, 4)]
    [InlineData(196, false, 4)]
    async Task should_write_the_supported_envelope_for_contextual_protection(int minor, bool declared, int version)
    {
        var (manager, _, _) = Create(minor);
        var schema = await ContextualSchema(declared);
        var plain = new JsonObject { ["value"] = "001" };
        var stored = await manager.Apply("store", "tenant", schema, "subject", plain);
        stored["value"].GetValue<string>().ShouldNotEqual("001");
        Version(stored).ShouldEqual(version);
        await ShouldRoundTrip(manager, schema, stored, plain);
    }

    [Fact]
    async Task should_keep_historic_plaintext_without_a_schema_kind()
    {
        var (manager, _, _) = Create(195);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":["string","number"],"compliance":[{"metadataType":"PII","details":""}]}}}""");
        var plain = new JsonObject { ["value"] = "historic" };
        await ShouldRoundTrip(manager, schema, plain, plain);
    }

    [Theory]
    [InlineData(178)]
    [InlineData(195)]
    async Task should_keep_erased_legacy_ciphertext_without_a_schema_kind(int minor)
    {
        var (manager, keys, _) = Create(minor);
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"compliance":[{"metadataType":"PII","details":""}]}}}""");
        var stored = await manager.Apply("store", "tenant", schema, "subject", new JsonObject { ["value"] = "secret" });
        await keys.RecordErasureFor("store", "tenant", "subject");
        await keys.DeleteFor("store", "tenant", "subject");
        var result = await manager.ReleaseWithStatus("store", "tenant", schema, "subject", stored);
        result.Statuses["value"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
        JsonNode.DeepEquals(result.Content, stored).ShouldBeTrue();
    }

    [Theory]
    [InlineData(195)]
    [InlineData(196)]
    async Task should_keep_concrete_ordinary_protection_in_an_indeterminate_document(int minor)
    {
        var (manager, _, _) = Create(minor);
        var definitions = new JsonObject { ["personal"] = JsonNode.Parse("""{"type":"string","compliance":[{"metadataType":"PII","details":""}]}""") };
        for (var index = 0; index < 4200; index++) definitions[$"plain{index}"] = new JsonObject { ["type"] = "string" };
        var schema = new JsonSchema(new JsonObject
        {
            ["type"] = "object",
            ["$defs"] = definitions,
            ["properties"] = JsonNode.Parse("""{"value":{"$ref":"#/$defs/personal"}}""")
        });
        var plain = new JsonObject { ["value"] = "secret" };
        var stored = await manager.Apply("store", "tenant", schema, "subject", plain);
        stored["value"].GetValue<string>().ShouldNotEqual("secret");
        await ShouldRoundTrip(manager, schema, stored, plain);
    }

    static (JsonSchemaMetadataManager Manager, InMemoryEncryptionKeyStorage Keys, RecordingLogger Logger) Create(int minor)
    {
        var encryption = LegacyEncryption.CreateAt(new SemanticVersion(19, minor, 0));
        var keys = new InMemoryEncryptionKeyStorage();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        var logger = new RecordingLogger();
        var handlers = new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
            new PIICompliancePropertyValueHandler(provisioner, keys, encryption),
            new EncryptedGlobalValueHandler(provisioner, keys, encryption));
        var manager = new JsonSchemaMetadataManager(handlers, logger);
        return (manager, keys, logger);
    }

    static async Task<JsonSchema> ContextualSchema(bool declared)
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","$defs":{"personal":{"$dynamicAnchor":"personal","type":"string","compliance":[{"metadataType":"PII","details":""}]}},
             "properties":{"value":{"$dynamicRef":"#personal"}}}
            """);
        if (declared) schema.Properties["value"].Type = JsonObjectType.String;
        return schema;
    }

    static int Version(JsonObject stored) => EncryptionEnvelope.Read(Convert.FromBase64String(stored["value"].GetValue<string>())).Version;

    static async Task ShouldRoundTrip(JsonSchemaMetadataManager manager, JsonSchema schema, JsonObject stored, JsonObject plain)
    {
        JsonNode.DeepEquals(await manager.Release("store", "tenant", schema, "subject", stored), plain).ShouldBeTrue();
        JsonNode.DeepEquals(await manager.ReleaseStrict("store", "tenant", schema, "subject", stored), plain).ShouldBeTrue();
        var comparison = await manager.TryRelease("store", "tenant", schema, "subject", stored);
        if (JsonNode.DeepEquals(stored, plain)) comparison.ShouldBeNull();
        else JsonNode.DeepEquals(comparison, plain).ShouldBeTrue();
        JsonNode.DeepEquals((await manager.ReleaseWithStatus("store", "tenant", schema, "subject", stored)).Content, plain).ShouldBeTrue();
    }

    sealed class RecordingLogger : ILogger<JsonSchemaMetadataManager>
    {
        internal List<KeyValuePair<string, object?>[]> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning && state is IEnumerable<KeyValuePair<string, object?>> fields)
            {
                Warnings.Add(fields.ToArray());
            }
        }
    }
}
