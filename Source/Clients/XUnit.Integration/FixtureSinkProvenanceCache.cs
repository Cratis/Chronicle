// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Gives each destructive fixture reset its own provenance observation lifetime.
/// </summary>
internal sealed class FixtureSinkProvenanceCache : ISinkProvenanceCache, IAsyncDisposable
#if DEVELOPMENT
    , IDevelopmentSinkProvenanceObservations
#endif
{
#if DEVELOPMENT
    readonly DevelopmentSinkProvenanceCache _cache = new();
#else
    SinkProvenanceCache _cache = new();
#endif

    /// <inheritdoc/>
    public ISinkProvenanceStorage Bind(object route, Func<ISinkProvenanceStorage> create) => _cache.Bind(route, create);

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _cache.DisposeAsync();

#if DEVELOPMENT
    Task IDevelopmentSinkProvenanceObservations.RotateAfterWipe() => Reset();
#endif

    /// <summary>
    /// Drains old observations after fixture storage is wiped, before kernel bootstrap.
    /// </summary>
    /// <returns>The reset task.</returns>
#if DEVELOPMENT
    internal Task Reset() => _cache.RotateAfterWipe();
#else
    internal async Task Reset()
    {
        var previous = Interlocked.Exchange(ref _cache, new SinkProvenanceCache());
        await previous.DisposeAsync();
    }
#endif
}
