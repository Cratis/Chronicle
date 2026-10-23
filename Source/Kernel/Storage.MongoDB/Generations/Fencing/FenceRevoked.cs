// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// The exception that is thrown when a fenced write is refused because its owner epoch has been revoked.
/// </summary>
/// <param name="token">The <see cref="FenceToken"/> that is no longer current.</param>
public class FenceRevoked(FenceToken token)
    : Exception($"The writer fence for '{token.GateId}' no longer holds epoch {token.Epoch}; the write was not committed.")
{
    /// <summary>
    /// Gets the <see cref="FenceToken"/> that is no longer current.
    /// </summary>
    public FenceToken Token { get; } = token;
}
