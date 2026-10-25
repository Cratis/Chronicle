// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Holds the open bulk windows of generation-bound observers for the whole silo. The projection handlers are
/// transient and the observer service resolves them again for every lifecycle call, so a window can only
/// survive between begin and end in a store that outlives them.
/// </summary>
[IgnoreConvention]
public sealed class GenerationBulkWindowStore
{
    readonly ConcurrentDictionary<(GenerationBulkKind Kind, ObserverKey Key), GenerationBulkWindow> _windows = new();

    /// <summary>Gets an open window.</summary>
    /// <param name="kind">The window kind.</param>
    /// <param name="key">The observer key.</param>
    /// <param name="window">The open window, when one exists.</param>
    /// <returns>True when a window exists.</returns>
    internal bool TryGet(GenerationBulkKind kind, ObserverKey key, out GenerationBulkWindow? window) =>
        _windows.TryGetValue((kind, key), out window);

    /// <summary>Sets an open window.</summary>
    /// <param name="kind">The window kind.</param>
    /// <param name="key">The observer key.</param>
    /// <param name="window">The window to store.</param>
    internal void Set(GenerationBulkKind kind, ObserverKey key, GenerationBulkWindow window) => _windows[(kind, key)] = window;

    /// <summary>Gets an open window, or adds the supplied one when none exists.</summary>
    /// <param name="kind">The window kind.</param>
    /// <param name="key">The observer key.</param>
    /// <param name="window">The window to add when none exists.</param>
    /// <returns>The existing or added window.</returns>
    internal GenerationBulkWindow GetOrAdd(GenerationBulkKind kind, ObserverKey key, GenerationBulkWindow window) =>
        _windows.GetOrAdd((kind, key), window);

    /// <summary>Removes an open window.</summary>
    /// <param name="kind">The window kind.</param>
    /// <param name="key">The observer key.</param>
    internal void Remove(GenerationBulkKind kind, ObserverKey key) => _windows.TryRemove((kind, key), out _);
}
