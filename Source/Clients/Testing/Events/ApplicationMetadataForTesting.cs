// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Shares the immutable application metadata of equivalent catalogs between testing event stores.
/// </summary>
internal static class ApplicationMetadataForTesting
{
    static readonly ConcurrentDictionary<ApplicationMetadataKey, ApplicationMetadata> _metadata = new();

    /// <summary>
    /// Gets or creates the metadata for the generator, naming policy, event store and catalogs.
    /// </summary>
    /// <param name="generator">The shared schema generator.</param>
    /// <param name="namingPolicy">The naming policy.</param>
    /// <param name="eventStoreName">The name of the event store.</param>
    /// <param name="artifacts">The artifacts the metadata is discovered from.</param>
    /// <returns>The shared <see cref="ApplicationMetadata"/>. Nothing is discovered until a store asks for it, so a failure is never cached.</returns>
    internal static ApplicationMetadata GetOrCreate(JsonSchemaGenerator generator, INamingPolicy namingPolicy, string eventStoreName, IClientArtifactsProvider artifacts) =>
        _metadata.GetOrAdd(new ApplicationMetadataKey(generator, namingPolicy, eventStoreName, artifacts), static _ => new ApplicationMetadata());
}
