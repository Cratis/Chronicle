// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// A pass-through handler for the benchmark metadata type, so the benchmarks measure the walk and not encryption.
/// </summary>
public class BenchmarkValueHandler : IJsonSchemaMetadataValueHandler
{
    /// <summary>The metadata type name the handler is registered for.</summary>
    public const string MetadataType = "bench-pii";

    /// <inheritdoc/>
    public SchemaMetadataCategory Category => SchemaMetadataCategory.Compliance;

    /// <inheritdoc/>
    public SchemaMetadataTypeName Type => MetadataType;

    /// <inheritdoc/>
    public Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) => Task.FromResult(value);

    /// <inheritdoc/>
    public Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) => Task.FromResult(value);
}
