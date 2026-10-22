// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// The exception that is thrown when a schema operation ran while its owner's epoch was being revoked.
/// </summary>
/// <param name="token">The <see cref="FenceToken"/> the operation was ordered behind.</param>
/// <remarks>
/// The operation has run and cannot be undone by the fence; the caller must treat its effect as made by a revoked owner.
/// </remarks>
public class FencedDdlRacedRevocation(FenceToken token)
    : Exception($"A schema operation guarded by '{token.GateId}' epoch {token.Epoch} ran while the epoch was revoked; its effect must be treated as unowned.")
{
    /// <summary>
    /// Gets the <see cref="FenceToken"/> the operation was ordered behind.
    /// </summary>
    public FenceToken Token { get; } = token;
}
