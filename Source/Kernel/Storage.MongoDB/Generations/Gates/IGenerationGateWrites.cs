// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Gates;

/// <summary>Writes gate epochs in the caller's storage transaction.</summary>
public interface IGenerationGateWrites
{
    /// <summary>Revokes a gate, creating it when necessary.</summary>
    /// <param name="database">The read-model database.</param>
    /// <param name="session">The publication session.</param>
    /// <param name="gate">The generation gate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new epoch.</returns>
    Task<long> Revoke(IMongoDatabase database, IClientSessionHandle session, GenerationGateId gate, CancellationToken ct = default);

    /// <summary>Revokes a raw writer gate without initializing its admission counter.</summary>
    /// <param name="database">The read-model database.</param>
    /// <param name="session">The writer's session.</param>
    /// <param name="gateId">The gate identity.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new epoch.</returns>
    Task<long> Revoke(IMongoDatabase database, IClientSessionHandle session, string gateId, CancellationToken ct = default);

    /// <summary>Reads the majority-committed epoch.</summary>
    /// <param name="database">The read-model database.</param>
    /// <param name="gate">The generation gate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The epoch, or null for an unclaimed gate.</returns>
    Task<long?> CurrentEpoch(IMongoDatabase database, GenerationGateId gate, CancellationToken ct = default);
}
