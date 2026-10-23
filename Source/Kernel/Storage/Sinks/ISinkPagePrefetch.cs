// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional read-only page prefetch capability for a sink's open bulk window.
/// </summary>
public interface ISinkPagePrefetch
{
    /// <summary>
    /// Prefetches distinct root keys, remembering page-scoped misses without changing write planning.
    /// Does nothing outside bulk mode.
    /// </summary>
    /// <param name="keys">The page's root keys.</param>
    /// <returns>A task representing the read-only prefetch.</returns>
    Task PrefetchPage(IEnumerable<Key> keys);

    /// <summary>
    /// Clears the page's known misses. States already in the bulk cache remain available.
    /// </summary>
    void ClearPrefetchPage();
}
