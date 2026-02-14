// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the result of a key resolution operation.
/// </summary>
public abstract record KeyResolverResult
{
    /// <summary>
    /// Creates a resolved result with a key.
    /// </summary>
    /// <param name="key">The resolved key.</param>
    /// <returns>A <see cref="ResolvedKey"/> result.</returns>
    public static KeyResolverResult Resolved(Key key) => new ResolvedKey(key);

    /// <summary>
    /// Creates a deferred result with a future.
    /// </summary>
    /// <param name="future">The projection future.</param>
    /// <returns>A <see cref="DeferredKey"/> result.</returns>
    public static KeyResolverResult Deferred(ProjectionFuture future) => new DeferredKey(future);
}
