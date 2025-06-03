// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Defines the chronicle fiture.
/// </summary>
public interface IChronicleFixture : IAsyncDisposable
{
    /// <summary>
    /// Get the container network.
    /// </summary>
    INetwork Network { get; }

    /// <summary>
    /// Get the MongoDB container.
    /// </summary>
    IContainer MongoDBContainer { get; }

    /// <summary>
    /// Gets the event store database.
    /// </summary>
    MongoDBDatabase EventStore { get; }

    /// <summary>
    /// Gets the event store database for the namespace used in the event store.
    /// </summary>
    MongoDBDatabase EventStoreForNamespace { get; }

    /// <summary>
    /// Gets the read models database.
    /// </summary>
    MongoDBDatabase ReadModels { get; }

    /// <summary>
    /// Performs a backup of the MongoDB database.
    /// </summary>
    /// <param name="prefix">The prefix to use in the filename.</param>
    void PerformBackup(string? prefix = null);

    /// <summary>
    /// Clears all databases in the MongoDB container.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllDatabases();
}