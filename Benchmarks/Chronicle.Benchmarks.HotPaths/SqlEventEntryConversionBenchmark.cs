// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Measures the complete in-memory SQL read conversion, including registered generation schema conversion.
/// No database, container, or kernel is required.
/// </summary>
[MemoryDiagnoser]
public class SqlEventEntryConversionBenchmark
{
    readonly IExpandoObjectConverter _converter = new ExpandoObjectConverter(new TypeFormats());
    readonly IIdentityStorage _identities = BenchmarkNoOpProxy.Bind<IIdentityStorage>(_ => Task.FromResult(Identity.NotSet));
    EventEntry _entry = null!;
    Func<EventType, Task<JsonSchema?>> _resolveSchema = null!;

    /// <summary>
    /// Gets or sets the number of nested items in the selected generation.
    /// </summary>
    [Params(1, 32)]
    public int ItemCount { get; set; }

    /// <summary>
    /// Prepares deterministic stored generations and a cached schema resolver outside the measured operation.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var schema = JsonSchema.FromJson(
            """{"type":"object","properties":{"id":{"type":"string","format":"uuid"},""" +
            """ "occurred":{"type":"string","format":"date-time-offset"},"amount":{"type":"number","format":"decimal"},""" +
            """ "items":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"count":{"type":"integer","format":"int32"},""" +
            """ "labels":{"type":"array","items":{"type":"string"}}}}},"metadata":{"type":"object","additionalProperties":{}}}}""");
        var resolved = Task.FromResult<JsonSchema?>(schema);
        _resolveSchema = _ => resolved;
        var items = string.Join(',', Enumerable.Repeat("""{"name":"sample item","count":42,"labels":["first","second"]}""", ItemCount));
        _entry = new EventEntry
        {
            Type = "sample",
            CorrelationId = Guid.Empty.ToString(),
            ContentHashes = """{"1":"original-hash","2":"current-hash"}""",
            Content = """{"1":{"name":"original"},"2":{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","occurred":"2025-01-02T03:04:05.1234567+05:30","amount":12345678901234567890.123456789,"items":[""" +
                items + """],"metadata":{"enabled":true,"count":3,"empty":null}}}"""
        };
    }

    /// <summary>
    /// Converts a stored event and its selected generation with the production converter.
    /// </summary>
    /// <returns>The converted event.</returns>
    [Benchmark]
    public Task<AppendedEvent> ConvertWithSchema() => EventEntryConverter.ToAppendedEvent(_entry, "store", "namespace", _identities, [], _resolveSchema, _converter);
}
