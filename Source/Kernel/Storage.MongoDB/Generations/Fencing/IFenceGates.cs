// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines the owner side of the writer fence: claiming and revoking the right to write a materialization generation.
/// </summary>
public interface IFenceGates
{
    /// <summary>
    /// Claims ownership of a materialization generation by bumping its epoch, which revokes any previous owner.
    /// </summary>
    /// <param name="materializationKey">The key identifying the materialization.</param>
    /// <param name="generation">The generation to claim.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The <see cref="FenceToken"/> granting the new owner the right to write.</returns>
    Task<FenceToken> Claim(string materializationKey, ulong generation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the current owner of a materialization generation by bumping its epoch in a transaction.
    /// </summary>
    /// <param name="materializationKey">The key identifying the materialization.</param>
    /// <param name="generation">The generation to revoke.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The epoch the gate holds after revocation.</returns>
    /// <remarks>
    /// Revocation serializes behind any batch already holding the gate: it commits only once that batch has committed or aborted,
    /// and no batch admitted under an older epoch can commit after it.
    /// </remarks>
    Task<long> Revoke(string materializationKey, ulong generation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the epoch a gate holds now, as committed by a majority.
    /// </summary>
    /// <param name="materializationKey">The key identifying the materialization.</param>
    /// <param name="generation">The generation of the gate.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The current epoch, or null if the gate has never been claimed.</returns>
    Task<long?> CurrentEpoch(string materializationKey, ulong generation, CancellationToken cancellationToken = default);
}
