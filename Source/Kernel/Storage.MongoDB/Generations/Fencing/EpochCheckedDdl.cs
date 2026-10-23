// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Checks the owner's epoch before and after a schema operation that MongoDB cannot run inside a transaction. This is detection,
/// not a fence.
/// </summary>
/// <param name="gates">The <see cref="IFenceGates"/> to read the epoch from.</param>
/// <remarks>
/// Dropping or renaming a collection cannot join the fenced transaction, so nothing makes it atomic with the gate. The pre-check
/// refuses to start for an owner that is already revoked, and the post-check reports a revocation that happened while the
/// operation ran. Neither prevents a stale operation: a drop delayed between the pre-check and the server executing it still
/// runs. Callers must not rely on this to protect another owner's data; destructive schema operations need exclusion (an
/// administrative path with access revoked first) or a layout where a stale owner's operation can only touch its own
/// generation's container.
/// </remarks>
public class EpochCheckedDdl(IFenceGates gates)
{
    /// <summary>
    /// Runs a schema operation for the owner of a token.
    /// </summary>
    /// <param name="token">The <see cref="FenceToken"/> of the owner.</param>
    /// <param name="operation">The schema operation.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="FenceRevoked">The epoch had moved on before the operation; it did not run.</exception>
    /// <exception cref="EpochCheckedDdlRacedRevocation">The epoch moved on while the operation ran.</exception>
    public async Task Run(FenceToken token, Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (await gates.CurrentEpoch(token.MaterializationKey, token.Generation, cancellationToken) != token.Epoch)
        {
            throw new FenceRevoked(token);
        }

        await operation();

        if (await gates.CurrentEpoch(token.MaterializationKey, token.Generation, cancellationToken) != token.Epoch)
        {
            throw new EpochCheckedDdlRacedRevocation(token);
        }
    }
}
