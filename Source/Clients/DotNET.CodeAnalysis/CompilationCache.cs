// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis;

/// <summary>
/// Holds one value per compilation, built once and shared by every analyzer that asks for it.
/// </summary>
/// <typeparam name="T">The type of value.</typeparam>
/// <remarks>
/// Entries are weakly keyed on the compilation, so they go away with it. A build cancelled by the caller that started it
/// is not cached: the next caller whose own token is live builds the value again.
/// </remarks>
internal sealed class CompilationCache<T>
{
    readonly ConditionalWeakTable<Compilation, Lazy<T>> _cache = new();

    /// <summary>
    /// Gets the value for a compilation, building it the first time it is asked for.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <param name="create">Builds the value for a compilation.</param>
    /// <param name="cancellationToken">The analysis cancellation token.</param>
    /// <returns>The shared value.</returns>
    internal T For(Compilation compilation, Func<Compilation, CancellationToken, T> create, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = _cache.GetValue(compilation, key => new Lazy<T>(() => create(key, cancellationToken)));
            try
            {
                return value.Value;
            }
            catch (OperationCanceledException)
            {
                // The shared Lazy caches the cancellation of whichever caller created it. Drop that entry
                // (only if it is still the one we observed) and let a caller whose own token is live rebuild it.
                if (_cache.TryGetValue(compilation, out var current) && ReferenceEquals(current, value))
                {
                    _cache.Remove(compilation);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }
    }
}
