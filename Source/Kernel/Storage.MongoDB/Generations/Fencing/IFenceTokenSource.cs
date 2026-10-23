// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines the source of the <see cref="FenceToken"/> a sink writes with.
/// </summary>
/// <remarks>
/// A stand-in for the generation binding: once generations are bound to owners, the binding hands out the token.
/// </remarks>
public interface IFenceTokenSource
{
    /// <summary>
    /// Gets the owner's current <see cref="FenceToken"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The <see cref="FenceToken"/>.</returns>
    Task<FenceToken> Current(CancellationToken cancellationToken = default);
}
