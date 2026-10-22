// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Registrations;
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
    /// <remarks>
    /// The event store name only changes the metadata when an event type in the catalog declares that store, so stores
    /// with any other name share one entry. Scenarios that give every store a unique name therefore still share, and do
    /// not grow the cache.
    /// </remarks>
    internal static ApplicationMetadata GetOrCreate(JsonSchemaGenerator generator, INamingPolicy namingPolicy, string eventStoreName, IClientArtifactsProvider artifacts) =>
        _metadata.GetOrAdd(
            new ApplicationMetadataKey(generator, namingPolicy, IsDeclaredBy(artifacts, eventStoreName) ? eventStoreName : null, artifacts),
            static _ => new ApplicationMetadata());

    static bool IsDeclaredBy(IClientArtifactsProvider artifacts, string eventStoreName)
    {
        try
        {
            return artifacts.EventTypes.Any(type => EventTypeDeclarations.ResolveEventStore(type, candidate => candidate.GetEventStoreName()) == eventStoreName);
        }
        catch (InvalidExplicitArtifact)
        {
            // A broken generation chain fails discovery itself; keep the name so nothing is shared for it.
            return true;
        }
    }
}
