// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>Measures lease checks in in-memory release scopes, not transport or database latency.</summary>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfiguration))]
public class ProtectedReleaseBenchmark
{
    CacheEncryptionKeyStorage _keys = null!;
    JsonSchemaMetadataManager _manager = null!;
    JsonSchema _schema = null!;
    JsonObject _protected = null!;
    AppendFingerprintInput _input = null!;
    StoredAppendFingerprint _fingerprint = null!;

    /// <summary>Gets or sets whether each operation starts with cold key material.</summary>
    [Params(false, true)]
    public bool Cold { get; set; }

    /// <summary>Build a fifty-property protected document sharing one identifier.</summary>
    [GlobalSetup]
    public void Setup() => Initialize().GetAwaiter().GetResult();

    /// <summary>Release one document.</summary>
    /// <returns>The released document.</returns>
    [Benchmark]
    public Task<JsonObject> SingleDocument()
    {
        EvictIfCold();
        return _manager.Release("store", "ns", _schema, "subject", _protected);
    }

    /// <summary>Release ten documents sharing the page's fence memo.</summary>
    /// <returns>The completed release.</returns>
    [Benchmark]
    public async Task Page()
    {
        EvictIfCold();
        using var scope = ReleaseKeys.BeginScope();
        for (var index = 0; index < 10; index++) await _manager.Release("store", "ns", _schema, "subject", _protected);
        await ReleaseKeys.Complete(() => true);
    }

    /// <summary>Release a status-bearing watch delta in its own scope.</summary>
    /// <returns>The status-bearing delta.</returns>
    [Benchmark]
    public Task<StatusedRelease> WatchDelta()
    {
        EvictIfCold();
        return _manager.ReleaseWithStatus("store", "ns", _schema, "subject", _protected);
    }

    /// <summary>Compare a protected append retry against its stored fingerprint.</summary>
    /// <returns>The completed comparison.</returns>
    [Benchmark]
    public async Task AppendRetry()
    {
        EvictIfCold();
        await AppendFingerprintComparison.Compare(_fingerprint, _input, _keys, _keys, "store", "ns");
    }

    async Task Initialize()
    {
        _keys = new(new InMemoryEncryptionKeyStorage(), TimeProvider.System);
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var properties = new JsonObject();
        var plaintext = new JsonObject();
        for (var index = 0; index < 50; index++)
        {
            properties[$"field{index}"] = new JsonObject
            {
                ["type"] = "string",
                ["compliance"] = new JsonArray(new JsonObject { ["metadataType"] = "PII", ["details"] = string.Empty })
            };
            plaintext[$"field{index}"] = "protected-value";
        }

        _schema = await JsonSchema.FromJsonAsync(new JsonObject { ["type"] = "object", ["properties"] = properties }.ToJsonString());
        var (value, uses) = await _manager.ApplyReportingKeys("store", "ns", _schema, "subject", plaintext);
        if (uses.Count != 50) throw new InvalidOperationException("Benchmark must protect all fifty properties.");
        _protected = value;
        var locations = await _manager.LocateProtectedValues(_schema, "subject", plaintext);
        _input = new(new EventType("event", 1), plaintext, "source", EventSourceType.Default, EventStreamType.All, EventStreamId.Default, null, "subject", "append-key:benchmark", locations);
        if (!AppendFingerprint.Compute(_input, uses).TryGetResult(out _fingerprint)) throw new InvalidOperationException("Benchmark fingerprint setup failed.");
    }

    void EvictIfCold()
    {
        if (Cold) _keys.EvictFromCache("store", "ns", "subject");
    }

    sealed class BenchmarkConfiguration : ManualConfig
    {
        public BenchmarkConfiguration() => AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
    }
}
