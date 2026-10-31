// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
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
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Measures append-time generation protection and unpinned delivery without external infrastructure.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfiguration))]
public class AppendGenerationProtectionBenchmark
{
    Fixture _single = null!;
    Fixture _plain = null!;
    Fixture _protected = null!;

    /// <summary>
    /// Prepares schemas, warms metadata caches, and provisions the subject's encryption key outside measurements.
    /// </summary>
    /// <returns>The setup task.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _single = await Fixture.Create(1, false);
        _plain = await Fixture.Create(3, false);
        _protected = await Fixture.Create(3, true);
        await _single.Migrate();
        await _plain.Migrate();
        await _protected.Migrate();
        await _single.Release();
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
        _plain.Registry.Dispose();
        _protected.Registry.Dispose();
    }

    // In-process execution is deliberate: copied baseline/final binaries must not rebuild against current sources.
    sealed class BenchmarkConfiguration : ManualConfig
    {
        public BenchmarkConfiguration() => AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
    }

    sealed class Fixture
    {
        const string Subject = "subject";
        static readonly EventStoreName Store = "store";
        static readonly EventStoreNamespaceName Namespace = "namespace";
        readonly JsonObject _plaintext = new() { ["token"] = "eyJhbGciOiJIUzI1NiJ9.personal-token.signature" };
        ProtectedEventTypeMigrations _migrations = null!;
        Events.EventGenerationRelease _release = null!;
        EventType _eventType = null!;
        ExpandoObject _source = null!;
        AppendedEvent[] _events = [];
        Dictionary<EventType, EventTypeSchema> _schemas = [];

        public EventStoreStorages Registry { get; } = new(new KnownInstancesOf<ISinkFactory>(), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>());

        public static async Task<Fixture> Create(uint generations, bool latestPii)
        {
            var fixture = new Fixture();
            await fixture.Initialize(generations, latestPii);
            return fixture;
        }

        public Task<IDictionary<EventTypeGeneration, ExpandoObject>> Migrate() =>
            _migrations.MigratePlaintext(Store, Namespace, _eventType, _plaintext, _source, Subject);

        public Task<AppendedEvent[]> Release() => _release.Release(Store, [], _schemas, _events);

        async Task Initialize(uint generations, bool latestPii)
        {
            var storage = new Storage.InMemory.Storage(Registry, new SystemStorage());
            var eventTypes = storage.GetEventStore(Store).EventTypes;
            var keys = new InMemoryEncryptionKeyStorage();
            var encryption = new Encryption();
            var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
            var manager = new JsonSchemaMetadataManager(
                new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
                NullLogger<JsonSchemaMetadataManager>.Instance);
            var converter = new ExpandoObjectConverter(new TypeFormats());
            _eventType = new("token-issued", generations);
            var schemas = new List<EventTypeGenerationDefinition>();
            for (uint generation = 1; generation <= generations; generation++)
            {
                var schema = await JsonSchema.FromJsonAsync(latestPii && generation == generations
                    ? """{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}"""
                    : """{"type":"object","properties":{"token":{"type":"string"}}}""");
                schemas.Add(new(generation, schema));
            }

            var migrations = generations == 1
                ? Array.Empty<EventTypeMigrationDefinition>()
                : [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())];
            await eventTypes.Register(new EventTypeDefinition(_eventType.Id, EventTypeOwner.None, false, schemas, migrations));
            var sourceSchema = schemas[^1].Schema;
            var protectedContent = await manager.Apply(Store, Namespace, sourceSchema, Subject, _plaintext);
            _source = converter.ToExpandoObject(protectedContent, sourceSchema);
            var engine = new EventTypeMigrations(storage, converter);
            _migrations = new(eventTypes, engine, manager, converter);
            _release = new(storage, new Events.EventCompliance(manager, converter), engine, manager, converter);
            _schemas = new() { [_eventType] = new(_eventType, EventTypeOwner.Server, EventTypeSource.Code, sourceSchema) };
            _events = [new(EventContext.Empty with { EventStore = Store, Namespace = Namespace, EventType = _eventType, Subject = Subject }, _source)];
        }
    }
}
