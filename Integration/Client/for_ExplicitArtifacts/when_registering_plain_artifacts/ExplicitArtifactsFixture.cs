// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration.for_ExplicitArtifacts.when_registering_plain_artifacts;

/// <summary>
/// Gives explicit-option specifications their own client host family while borrowing collection-owned infrastructure.
/// </summary>
/// <param name="infrastructure">The collection's infrastructure fixture.</param>
public class ExplicitArtifactsFixture(ChronicleFixture infrastructure) : IChronicleFixture
{
    /// <inheritdoc/>
    public INetwork Network => infrastructure.Network;

    /// <inheritdoc/>
    public IContainer MongoDBContainer => infrastructure.MongoDBContainer;

    /// <inheritdoc/>
    public MongoDBDatabase EventStore => infrastructure.EventStore;

    /// <inheritdoc/>
    public MongoDBDatabase EventStoreForNamespace => infrastructure.EventStoreForNamespace;

    /// <inheritdoc/>
    public MongoDBDatabase ReadModels => infrastructure.ReadModels;

    /// <inheritdoc/>
    public Task PerformBackupAsync(string? prefix = null) => infrastructure.PerformBackupAsync(prefix);

    /// <inheritdoc/>
    public Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null) => infrastructure.RemoveAllDatabases(excludePrefixes);

    /// <summary>
    /// Leaves collection-owned containers and network to the collection fixture's disposal.
    /// </summary>
    /// <returns>A completed disposal operation for this non-owning adapter.</returns>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
