// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Ordinary release and apply, the paths every protected read and append goes through.
/// </summary>
[MemoryDiagnoser]
public class JsonSchemaMetadataManagerBenchmark
{
    const string Identifier = "subject";
    readonly EventStoreName _eventStore = "store";
    readonly EventStoreNamespaceName _namespace = "ns";
    JsonSchemaMetadataManager _manager = null!;
    JsonSchema _withPii = null!;
    JsonSchema _withoutMetadata = null!;
    JsonObject _json = null!;

    /// <summary>
    /// Builds the manager, the schemas and the document.
    /// </summary>
    /// <returns>A task for async completion.</returns>
    [GlobalSetup]
    public async Task Setup()
    {
        _manager = new(new Handlers(), NullLogger<JsonSchemaMetadataManager>.Instance);
        _withPii = await JsonSchema.FromJsonAsync(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"compliance\":[{\"metadataType\":\"" + BenchmarkValueHandler.MetadataType + "\",\"details\":\"\"}]},\"number\":{\"type\":\"integer\"}}}");
        _withoutMetadata = await JsonSchema.FromJsonAsync(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"number\":{\"type\":\"integer\"}}}");
        _json = new JsonObject { ["name"] = "value", ["number"] = 42 };
    }

    /// <summary>Releases a document with one PII string property.</summary>
    /// <returns>The released document.</returns>
    [Benchmark]
    public Task<JsonObject> ReleaseWithPii() => _manager.Release(_eventStore, _namespace, _withPii, Identifier, _json);

    /// <summary>Applies a document with one PII string property.</summary>
    /// <returns>The applied document.</returns>
    [Benchmark]
    public Task<JsonObject> ApplyWithPii() => _manager.Apply(_eventStore, _namespace, _withPii, Identifier, _json);

    /// <summary>Releases a document whose schema has no metadata.</summary>
    /// <returns>The document.</returns>
    [Benchmark]
    public Task<JsonObject> ReleaseWithoutMetadata() => _manager.Release(_eventStore, _namespace, _withoutMetadata, Identifier, _json);

    /// <summary>Applies a document whose schema has no metadata.</summary>
    /// <returns>The document.</returns>
    [Benchmark]
    public Task<JsonObject> ApplyWithoutMetadata() => _manager.Apply(_eventStore, _namespace, _withoutMetadata, Identifier, _json);

    sealed class Handlers : IInstancesOf<IJsonSchemaMetadataValueHandler>
    {
        readonly IJsonSchemaMetadataValueHandler[] _handlers = [new BenchmarkValueHandler()];

        public IEnumerator<IJsonSchemaMetadataValueHandler> GetEnumerator() => ((IEnumerable<IJsonSchemaMetadataValueHandler>)_handlers).GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
