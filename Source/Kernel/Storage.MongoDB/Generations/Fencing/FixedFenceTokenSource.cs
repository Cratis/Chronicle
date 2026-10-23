// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Represents an <see cref="IFenceTokenSource"/> holding a token handed to it.
/// </summary>
/// <param name="token">The <see cref="FenceToken"/> to write with.</param>
public sealed class FixedFenceTokenSource(FenceToken token) : IFenceTokenSource
{
    /// <inheritdoc/>
    public Task<FenceToken> Current(CancellationToken cancellationToken = default) => Task.FromResult(token);
}
