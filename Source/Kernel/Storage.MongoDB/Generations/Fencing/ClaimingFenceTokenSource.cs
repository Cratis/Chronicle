// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an <see cref="IFenceTokenSource"/> that claims the gate once, the first time a token is asked for.
/// </summary>
/// <param name="gates">The <see cref="IFenceGates"/> to claim through.</param>
/// <param name="materializationKey">The key identifying the materialization.</param>
/// <param name="generation">The generation to claim.</param>
/// <remarks>
/// A stand-in until generations are bound to owners: the most recent writer to claim owns the gate, which revokes every earlier one.
/// </remarks>
public sealed class ClaimingFenceTokenSource(IFenceGates gates, string materializationKey, ulong generation) : IFenceTokenSource
{
    readonly Lazy<Task<FenceToken>> _token = new(() => gates.Claim(materializationKey, generation));

    /// <inheritdoc/>
    public Task<FenceToken> Current(CancellationToken cancellationToken = default) => _token.Value.WaitAsync(cancellationToken);
}
