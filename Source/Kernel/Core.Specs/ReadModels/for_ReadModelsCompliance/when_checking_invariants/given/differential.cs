// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants.given;

public static class differential
{
    static readonly encryption_vectors _encryption = new();
    static readonly EncryptionKey _key = new Encryption().GenerateKey();

    public static async Task Compare(ITestOutputHelper output, Func<(JsonSchema Schema, ExpandoObject State)> create, bool erased, bool pipeline = false, string subject = "matrix-subject", string[]? legacyPaths = null, bool reducer = false)
    {
        var main = await Observe(create, erased, pipeline, subject, legacyPaths, reducer, mode.Main);
        var current = await Observe(create, erased, pipeline, subject, legacyPaths, reducer, mode.Current);
        var expected = erased ? await Observe(create, erased, pipeline, subject, legacyPaths, reducer, mode.ErasedRelease) : main;
        Assert.Equal(main.Keys, current.Keys);
        Assert.Equal(expected.Keys, current.Keys);
        var differences = new List<string>();
        foreach (var (boundary, actual) in current)
        {
            Assert.True(JsonNode.DeepEquals(expected[boundary], actual), $"{boundary}: expected {expected[boundary]}, actual {actual}");
            if (JsonNode.DeepEquals(main[boundary], actual)) continue;
            Assert.True(erased && (boundary == "fresh_erased" || boundary == "reapplied_erased" || boundary == "reduced_erased"), $"Unexpected difference from main at {boundary}: main {main[boundary]}, actual {actual}");
            Assert.Contains(nameof(EncryptionKeyErased), main[boundary]!.ToJsonString(), StringComparison.Ordinal);
            if (actual?["errors"] is not null)
            {
                // A schema main already rejects may reveal that same rejection after the fence is handled.
                Assert.True(JsonNode.DeepEquals(main[boundary == "reduced_erased" ? "initial_reducer" : "initial"], actual), $"New read-model refusal at {boundary}: {actual}");
            }
            else
            {
                Assert.NotNull(actual?["stored"]);
            }
            differences.Add(boundary);
        }
        output.WriteLine(differences.Count == 0 ? "IDENTICAL" : $"DIFFERENT #4453: {string.Join(',', differences)}");
    }

    static async Task<Dictionary<string, JsonNode?>> Observe(Func<(JsonSchema Schema, ExpandoObject State)> create, bool erased, bool pipeline, string subject, string[]? legacyPaths, bool reducer, mode behavior)
    {
        var (schema, state) = create();
        var encryption = _encryption;
        var keys = new InMemoryEncryptionKeyStorage();
        await keys.SaveFor("store", "Default", "matrix-subject", _key);
        await keys.SaveFor("store", "Default", "subject", _key);
        await keys.SaveFor("store", "Default", string.Empty, _key);
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = Manager(handler);
        var converter = new ExpandoObjectConverter(new TypeFormats());

        // Main's read-model path calls Apply. Keep that entry point as the reference, with exactly
        // the same schema detection, walk and converter; no old-PR hardening or limitation whitelist.
        IJsonSchemaMetadataManager applying = behavior switch
        {
            mode.Main => new main_apply(manager),
            mode.ErasedRelease => new main_apply(Manager(new erased_release(handler, encryption))),
            _ => manager
        };
        var compliance = new ReadModelsCompliance(applying, converter);
        var observations = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        ExpandoObject? stored = null;
        ExpandoObject? released = null;
        JsonObject? appended = null;
        observations["metadata"] = JsonSerializer.SerializeToNode(new[] { schema.HasSchemaMetadata(), schema.HasSchemaMetadata(SchemaMetadataCategory.Security) });
        await Capture("input_release", async () => await compliance.Release("store", "Default", schema, state));
        await Capture("input_release_json", async () => await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(state)!.AsObject()));
        await Capture("append_raw", async () => await manager.Apply("store", "Default", schema, subject, JsonSerializer.SerializeToNode(state)!.AsObject()));
        await Capture("conversion", () => Task.FromResult<object>(converter.ToJsonObject(state, schema)));
        await Capture("append", async () => appended = await manager.Apply("store", "Default", schema, subject, converter.ToJsonObject(state, schema)));
        if (appended is not null) await Capture("append_release", async () => await manager.Release("store", "Default", schema, subject, appended));
        await Capture("initial", async () => stored = await Store(state));
        if (reducer) await Capture("initial_reducer", async () => await Update(create().State, reduce: true));
        if (stored is not null)
        {
            await Capture("initial_release", async () => await compliance.Release("store", "Default", schema, stored));
            await Capture("initial_release_json", async () => await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(stored)!.AsObject()));
        }
        var legacy = JsonSerializer.SerializeToNode(create().State)!.AsObject();
        foreach (var path in legacyPaths ?? [])
        {
            var segments = path.Split('.');
            JsonNode parent = legacy;
            foreach (var segment in segments[..^1])
                parent = parent is JsonArray array ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)]! : parent[segment]!;
            parent[segments[^1]] = await handler.Apply("store", "Default", subject, parent[segments[^1]]!);
        }
        await ReadLegacy("active");
        if (erased)
        {
            await keys.RecordErasureFor("store", "Default", "matrix-subject");
            await keys.DeleteFor("store", "Default", "matrix-subject");
            await keys.RecordErasureFor("store", "Default", "subject");
            await keys.DeleteFor("store", "Default", "subject");
            await ReadLegacy("erased");
            if (appended is not null) await Capture("erased_append_release", async () => await manager.Release("store", "Default", schema, subject, appended));
            await Capture("erased_append", async () => await manager.Apply("store", "Default", schema, subject, converter.ToJsonObject(create().State, schema)));
            if (stored is not null)
            {
                await Capture("erased_release", async () => released = await compliance.Release("store", "Default", schema, stored));
                await Capture("erased_release_json", async () => await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(stored)!.AsObject()));
            }
            var plaintext = create().State;
            ((IDictionary<string, object?>)plaintext)["__subject"] = subject;
            await Capture("plaintext_release", async () => await compliance.Release("store", "Default", schema, plaintext));
            await Capture("plaintext_release_json", async () => await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(plaintext)!.AsObject()));
            await Capture("fresh_erased", async () => await Update(create().State));
            if (reducer) await Capture("reduced_erased", async () => await Update(create().State, reduce: true));
            if (released is not null) await Capture("reapplied_erased", async () => await Update(released));
            Assert.False(await keys.HasFor("store", "Default", "matrix-subject"));
            Assert.False(await keys.HasFor("store", "Default", "subject"));
        }
        return observations;

        async Task ReadLegacy(string stage)
        {
            if (legacyPaths is not { Length: > 0 }) return;
            await Capture($"legacy_{stage}", async () => await manager.Release("store", "Default", schema, subject, legacy));
            var document = (JsonObject)legacy.DeepClone();
            document["__subject"] = subject;
            await Capture($"legacy_json_{stage}", async () => await compliance.ReleaseJson("store", "Default", schema, document));
            await Capture($"legacy_expando_{stage}", async () => await compliance.Release("store", "Default", schema, converter.ToExpandoObject(document, new JsonSchema())));
        }

        async Task<object> Update(ExpandoObject input, bool reduce = false)
        {
            var updated = reduce ? await reduction.Store(compliance, schema, input, subject) : await Store(input);
            return new JsonObject
            {
                ["stored"] = Normalize(JsonSerializer.SerializeToNode(updated)),
                ["released"] = await Outcome(async () => await compliance.Release("store", "Default", schema, updated)),
                ["released_json"] = await Outcome(async () => await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(updated)!.AsObject()))
            };
        }

        async Task Capture(string boundary, Func<Task<object>> action) => observations[boundary] = await Outcome(action);

        async Task<JsonNode?> Outcome(Func<Task<object>> action)
        {
            try
            {
                return Normalize(JsonSerializer.SerializeToNode(await action()));
            }
            catch (Exception exception)
            {
                var errors = new JsonArray();
                for (var error = exception; error is not null; error = error.InnerException)
                {
                    errors.Add($"{error.GetType().FullName}: {error.Message}");
                }
                return new JsonObject { ["errors"] = errors };
            }
        }

        JsonNode? Normalize(JsonNode? value)
        {
            // Compare ciphertext contents, not random IVs, and keep the protection envelope distinct
            // from plaintext. This catches value corruption as well as a new plaintext path.
            if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text) && ProtectedValueCodec.TryDecodeCipherText(encryption, text, out var cipher))
                return new JsonObject { ["ciphertext"] = ProtectedValueCodec.Decrypt(encryption, _key, cipher) };
            if (value is JsonObject document)
                return new JsonObject(document.Select(_ => new KeyValuePair<string, JsonNode?>(_.Key, Normalize(_.Value))));
            if (value is JsonArray array) return new JsonArray(array.Select(Normalize).ToArray());
            return value?.DeepClone();
        }

        async Task<ExpandoObject> Store(ExpandoObject input)
        {
            if (!pipeline) return await compliance.Apply("store", "Default", schema, subject, input);
            var comparer = new ObjectComparer();
            var projection = Substitute.For<IProjection>();
            projection.TargetReadModelSchema.Returns(schema);
            projection.InitialModelState.Returns(input);
            var @event = new AppendedEvent(EventContext.From("store", "Default", EventType.Unknown, EventSourceType.Default, subject, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet), new ExpandoObject());
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(comparer, @event, new ExpandoObject());
            var context = new ProjectionEventContext(new Key(subject, ArrayIndexers.NoIndexers), @event, changeset, ProjectionOperationType.None, false);
            context = await new SetInitialState(Substitute.For<ISink>(), NullLogger<SetInitialState>.Instance).Perform(projection, context);
            await new EncryptChangeset(compliance, comparer, "store", "Default").Perform(projection, context);
            return (ExpandoObject)changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().Last().State;
        }
    }

    static JsonSchemaMetadataManager Manager(IJsonSchemaMetadataValueHandler handler) =>
        new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);

    enum mode
    {
        Main = 0,
        Current = 1,
        ErasedRelease = 2
    }

    sealed class main_apply(JsonSchemaMetadataManager manager) : IJsonSchemaMetadataManager
    {
        public Task<JsonObject> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json) =>
            manager.Apply(eventStore, eventStoreNamespace, schema, identifier, json);

        public Task<JsonObject> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, JsonSchema schema, string identifier, JsonObject json) =>
            manager.Release(eventStore, eventStoreNamespace, schema, identifier, json);
    }

    sealed class erased_release(PIICompliancePropertyValueHandler handler, IEncryption encryption) : IJsonSchemaMetadataValueHandler
    {
        public SchemaMetadataCategory Category => handler.Category;
        public SchemaMetadataTypeName Type => handler.Type;

        public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
        {
            try
            {
                return await handler.Apply(eventStore, eventStoreNamespace, identifier, value);
            }
            catch (EncryptionKeyErased)
            {
                // Ask main's real release handler, rather than prescribing typed defaults from #4454.
                var ciphertext = ProtectedValueCodec.Encrypt(encryption, _key, value);
                return await handler.Release(eventStore, eventStoreNamespace, identifier, ciphertext);
            }
        }

        public Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
            handler.Release(eventStore, eventStoreNamespace, identifier, value);
    }
}
