// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Keeps provenance observations alive only until their development storage generation is wiped.
/// </summary>
/// <param name="timeProvider">The clock used by each generation.</param>
/// <param name="logger">The logger used by each generation.</param>
public sealed class DevelopmentSinkProvenanceCache(TimeProvider? timeProvider = null, ILogger<SinkProvenanceCache>? logger = null)
    : ISinkProvenanceCache, IDevelopmentSinkProvenanceObservations, IAsyncDisposable
{
    readonly Lock _lifetime = new();
    SinkProvenanceCache? _current = new(timeProvider, logger);
    Task _draining = Task.CompletedTask;

    /// <inheritdoc/>
    public ISinkProvenanceStorage Bind(object route, Func<ISinkProvenanceStorage> create)
    {
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_current is null, this);
            return _current.Bind(route, create);
        }
    }

    /// <inheritdoc/>
    public Task RotateAfterWipe()
    {
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_current is null, this);
            _draining = Task.WhenAll(_draining, _current.DisposeAsync().AsTask());
            _current = new(timeProvider, logger);
            return _draining;
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_lifetime)
        {
            if (_current is not null)
            {
                _draining = Task.WhenAll(_draining, _current.DisposeAsync().AsTask());
                _current = null;
            }

            return new(_draining);
        }
    }
}
#endif
