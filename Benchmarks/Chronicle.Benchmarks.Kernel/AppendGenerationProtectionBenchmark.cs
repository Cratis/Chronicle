// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures append-time generation protection and unpinned delivery without external infrastructure.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfiguration))]
public class AppendGenerationProtectionBenchmark
{
    Fixture _single = null!;
    Fixture _singleProtected = null!;
    Fixture _plain = null!;
    Fixture _protected = null!;
    Fixture _inherited = null!;
    Fixture _wideNoPii = null!;

    /// <summary>
    /// Prepares schemas, warms metadata caches, and provisions the subject's encryption key outside measurements.
    /// </summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _single = await Fixture.Create(1, false);
        _singleProtected = await Fixture.Create(1, true);
        _plain = await Fixture.Create(3, false);
        _protected = await Fixture.Create(3, true);
        _inherited = await Fixture.Create(3, true, appendFirst: true, fieldCount: 24);
        _wideNoPii = await Fixture.Create(3, false, appendFirst: true, fieldCount: 24);
        await _single.Migrate();
        await _plain.Migrate();
        await _protected.Migrate();
        await _singleProtected.ProtectAndMigrate();
        await _protected.ProtectAndMigrate();
        await _single.Release();
        await _inherited.ProtectAndMigrate();
        await _wideNoPii.ProtectAndMigrate();
    }

    /// <summary>
    /// Measures the common single-generation append.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> OneGeneration() => _single.Migrate();

    /// <summary>
    /// Measures three generations without classified properties.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> ThreeGenerationsNoPii() => _plain.Migrate();

    /// <summary>
    /// Measures downcasting from generation three, where only its string property is classified as PII.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> ThreeGenerationsLatestPii() => _protected.Migrate();

    /// <summary>
    /// Measures source protection and migration for a single generation with PII.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> OneGenerationFullAppendPii() => _singleProtected.ProtectAndMigrate();

    /// <summary>
    /// Measures source protection and migration when only generation three classifies its string property as PII.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> ThreeGenerationsFullAppendLatestPii() => _protected.ProtectAndMigrate();

    /// <summary>
    /// Measures source protection and migration from generation one with inherited PII and a 25-property schema.
    /// </summary>
    /// <returns>The protected generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> AppendedOlderGeneration() => _inherited.ProtectAndMigrate();

    /// <summary>
    /// Measures the same wide-schema append boundary without any classified properties.
    /// </summary>
    /// <returns>The unchanged generations.</returns>
    [Benchmark]
    public Task<IDictionary<EventTypeGeneration, ExpandoObject>> WideSchemaFullAppendNoPii() => _wideNoPii.ProtectAndMigrate();

    /// <summary>
    /// Measures 100 sequential lower-generation append protection and migration boundaries.
    /// </summary>
    /// <returns>The total generation count.</returns>
    [Benchmark]
    public Task<int> Batch100AppendedOlderGeneration() => _inherited.ProtectAndMigrateBatch();

    /// <summary>
    /// Measures 100 sequential wide-schema non-PII append protection and migration boundaries.
    /// </summary>
    /// <returns>The total generation count.</returns>
    [Benchmark]
    public Task<int> Batch100WideSchemaNoPii() => _wideNoPii.ProtectAndMigrateBatch();

    /// <summary>
    /// Measures release of one unpinned event with no classified properties.
    /// </summary>
    /// <returns>The released events.</returns>
    [Benchmark]
    public Task<AppendedEvent[]> UnpinnedRelease() => _single.Release();

    /// <summary>
    /// Releases the in-memory registries after the measurements.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _single.Registry.Dispose();
        _singleProtected.Registry.Dispose();
        _plain.Registry.Dispose();
        _protected.Registry.Dispose();
        _inherited.Registry.Dispose();
        _wideNoPii.Registry.Dispose();
    }

    /// <summary>
    /// Runs copied baseline/final binaries in-process without rebuilding against current sources.
    /// </summary>
    sealed class BenchmarkConfiguration : ManualConfig
    {
        public BenchmarkConfiguration() => AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
    }

    sealed class Fixture
    {
        const string Subject = "subject";
        static readonly EventStoreName _store = "store";
        static readonly EventStoreNamespaceName _namespace = "namespace";
        readonly JsonObject _plaintext = new() { ["token"] = "eyJhbGciOiJIUzI1NiJ9.personal-token.signature" };
        ProtectedEventTypeMigrations _migrations = null!;
        JsonSchemaMetadataManager _manager = null!;
        ExpandoObjectConverter _converter = null!;
        JsonSchema _sourceSchema = null!;
        IEventTypeSchemaCache _schemaCache = null!;
        EventGenerationRelease _release = null!;
        EventType _eventType = null!;
        ExpandoObject _source = null!;
        AppendedEvent[] _events = [];
        Dictionary<EventType, EventTypeSchema> _schemas = [];

        public EventStoreStorages Registry { get; } = new(new KnownInstancesOf<ISinkFactory>(), new InMemoryJobsStorage(JobTypes.Instance));

        public static async Task<Fixture> Create(uint generations, bool latestPii, bool appendFirst = false, int fieldCount = 0)
        {
            var fixture = new Fixture();
            await fixture.Initialize(generations, latestPii, appendFirst, fieldCount);
            return fixture;
        }

        public Task<IDictionary<EventTypeGeneration, ExpandoObject>> Migrate() =>
            _migrations.MigratePlaintext(_store, _namespace, _eventType, _plaintext, _source, Subject);

        public async Task<IDictionary<EventTypeGeneration, ExpandoObject>> ProtectAndMigrate()
        {
            var effective = await _schemaCache.GetProtectionSchemaFor(_store, _eventType.Id, _eventType.Generation, _sourceSchema);
            var protectedContent = await _manager.Apply(_store, _namespace, effective, Subject, _plaintext);
            var source = _converter.ToExpandoObject(protectedContent, effective);
            return await _migrations.MigratePlaintext(_store, _namespace, _eventType, _plaintext, source, Subject);
        }

        public async Task<int> ProtectAndMigrateBatch()
        {
            var count = 0;
            for (var index = 0; index < 100; index++)
            {
                count += (await ProtectAndMigrate()).Count;
            }

            return count;
        }

        public Task<AppendedEvent[]> Release() => _release.Release(_store, [], _schemas, _events);

        static void AddSchemaFields(JsonSchema schema, int fieldCount)
        {
            for (var index = 0; index < fieldCount; index++)
            {
                schema.Properties.Add($"field{index}", new JsonSchemaProperty { Type = JsonObjectType.String });
            }
        }

        async Task Initialize(uint generations, bool latestPii, bool appendFirst, int fieldCount)
        {
            var storage = new Storage.InMemory.Storage(Registry, new SystemStorage());
            var eventTypes = storage.GetEventStore(_store).EventTypes;
            var keys = new InMemoryEncryptionKeyStorage();
            var encryption = new Encryption();
            var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
            var manager = new JsonSchemaMetadataManager(
                new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
                NullLogger<JsonSchemaMetadataManager>.Instance);
            var converter = new ExpandoObjectConverter(new TypeFormats());
            _eventType = new("token-issued", appendFirst ? 1u : generations);
            AddPayloadFields(fieldCount);
            var schemas = new List<EventTypeGenerationDefinition>();
            for (uint generation = 1; generation <= generations; generation++)
            {
                var schema = await JsonSchema.FromJsonAsync(latestPii && generation == generations
                    ? """{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}"""
                    : """{"type":"object","properties":{"token":{"type":"string"}}}""");
                AddSchemaFields(schema, fieldCount);
                schemas.Add(new(generation, schema));
            }

            var migrations = generations == 1
                ? Array.Empty<EventTypeMigrationDefinition>()
                : [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())];
            var definition = new EventTypeDefinition(_eventType.Id, EventTypeOwner.None, false, schemas, migrations);
            await eventTypes.Register(definition);
            var sourceSchema = schemas.Single(_ => _.Generation == _eventType.Generation).Schema;
            _manager = manager;
            _converter = converter;
            _sourceSchema = sourceSchema;
            _schemaCache = new EventTypeSchemaCache(storage);
            var effective = await _schemaCache.GetProtectionSchemaFor(_store, _eventType.Id, _eventType.Generation, sourceSchema);
            var protectedContent = await manager.Apply(_store, _namespace, effective, Subject, _plaintext);
            _source = converter.ToExpandoObject(protectedContent, effective);
            var engine = new EventTypeMigrations(storage, converter);
            _migrations = new(eventTypes, engine, manager, converter);
            if (latestPii)
            {
                await VerifyProtection(definition);
            }
            _release = new(storage, new EventCompliance(manager, converter, _schemaCache), engine, manager, converter, _schemaCache);
            _schemas = new() { [_eventType] = new(_eventType, EventTypeOwner.Server, EventTypeSource.Code, sourceSchema) };
            _events = [new(EventContext.Empty with { EventStore = _store, Namespace = _namespace, EventType = _eventType, Subject = Subject }, _source)];
        }

        void AddPayloadFields(int fieldCount)
        {
            for (var index = 0; index < fieldCount; index++)
            {
                _plaintext[$"field{index}"] = $"representative-value-{index}";
            }
        }

        async Task VerifyProtection(EventTypeDefinition definition)
        {
            foreach (var marker in new[] { "first-personal-marker", "second-personal-marker" })
            {
                var plaintext = new JsonObject { ["token"] = marker };
                var effective = await _schemaCache.GetProtectionSchemaFor(_store, _eventType.Id, _eventType.Generation, _sourceSchema);
                var protectedSource = _converter.ToExpandoObject(await _manager.Apply(_store, _namespace, effective, Subject, plaintext), effective);
                var protectedGenerations = await _migrations.MigratePlaintext(_store, _namespace, _eventType, plaintext, protectedSource, Subject);
                foreach (var (generation, content) in protectedGenerations)
                {
                    await VerifyGeneration(definition, generation, content, marker);
                }
            }
        }

        async Task VerifyGeneration(EventTypeDefinition definition, EventTypeGeneration generation, ExpandoObject content, string marker)
        {
            var json = JsonSerializer.SerializeToNode(content)!.AsObject();

            var released = await _manager.ReleaseStrict(_store, _namespace, GenerationProtectionSchemas.For(definition, generation), Subject, json);
            if (json.ToJsonString().Contains(marker, StringComparison.Ordinal) || released["token"]!.GetValue<string>() != marker)
            {
                throw new InvalidOperationException("Generation protection must encrypt and release each call's own marker.");
            }
        }
    }
}
