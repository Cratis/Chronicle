// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Orders a schema operation that MongoDB cannot run inside a transaction behind an epoch check, and checks the epoch again after it.
/// </summary>
/// <param name="gates">The <see cref="IFenceGates"/> to read the epoch from.</param>
/// <remarks>
/// Dropping or renaming a collection cannot join the fenced transaction, so it cannot be made atomic with the gate. This is the
/// weaker guarantee that remains: it refuses to start for a revoked owner, and reports a revocation that raced it so the caller can
/// compensate. It does not stop a revoked owner's operation that was already running.
/// </remarks>
public class FencedDdl(IFenceGates gates)
{
    /// <summary>
    /// Runs a schema operation for the owner of a token.
    /// </summary>
    /// <param name="token">The <see cref="FenceToken"/> of the owner.</param>
    /// <param name="operation">The schema operation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="FenceRevoked">The epoch had moved on before the operation; it did not run.</exception>
    /// <exception cref="FencedDdlRacedRevocation">The epoch moved on while the operation ran.</exception>
    public async Task Run(FenceToken token, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (await gates.CurrentEpoch(token.MaterializationKey, token.Generation, cancellationToken) != token.Epoch)
        {
            throw new FenceRevoked(token);
        }

        await operation();

        if (await gates.CurrentEpoch(token.MaterializationKey, token.Generation, cancellationToken) != token.Epoch)
        {
            throw new FencedDdlRacedRevocation(token);
        }
    }
}
