// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Caches positive namespace casing resolutions for the lifetime of a silo.
/// </summary>
/// <param name="grainFactory">The grain factory.</param>
/// <param name="logger">Logger for case variant resolutions.</param>
[Singleton]
public class NamespaceCasing(IGrainFactory grainFactory, ILogger<NamespaceCasing> logger) : INamespaceCasing
{
    readonly ConcurrentDictionary<EventStoreName, ConcurrentDictionary<string, EventStoreNamespaceName>> _names = new();
    readonly ConcurrentDictionary<(EventStoreName Store, string Requested), byte> _logged = new();
    readonly ConcurrentDictionary<EventStoreName, long> _versions = new();

    /// <inheritdoc/>
    public ValueTask<EventStoreNamespaceName> Canonicalize(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (@namespace.Value.Equals(EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase))
        {
            Log(eventStore, @namespace, EventStoreNamespaceName.Default);
            return ValueTask.FromResult(EventStoreNamespaceName.Default);
        }
        var names = _names.GetOrAdd(eventStore, _ => new(StringComparer.OrdinalIgnoreCase));
        if (names.TryGetValue(@namespace.Value, out var canonical))
        {
            Log(eventStore, @namespace, canonical);
            return ValueTask.FromResult(canonical);
        }
        return Resolve(eventStore, @namespace, names);
    }

    /// <inheritdoc/>
    public void Forget(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var names = _names.GetOrAdd(eventStore, _ => new(StringComparer.OrdinalIgnoreCase));
        lock (names)
        {
            _versions.AddOrUpdate(eventStore, 1, (_, version) => version + 1);
            names.TryRemove(@namespace.Value, out _);
            foreach (var key in _logged.Keys.Where(key => key.Store == eventStore && key.Requested.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase)))
            {
                _logged.TryRemove(key, out _);
            }
        }
    }

    async ValueTask<EventStoreNamespaceName> Resolve(EventStoreName eventStore, EventStoreNamespaceName @namespace, ConcurrentDictionary<string, EventStoreNamespaceName> names)
    {
        var version = _versions.GetOrAdd(eventStore, 0);
        var canonical = await grainFactory.GetGrain<INamespaces>(eventStore).GetCanonical(@namespace);
        if (canonical is null) return @namespace;
        lock (names)
        {
            if (_versions.GetOrAdd(eventStore, 0) == version)
            {
                names.TryAdd(@namespace.Value, canonical);
                Log(eventStore, @namespace, canonical);
            }
        }
        return canonical;
    }

    void Log(EventStoreName eventStore, EventStoreNamespaceName requested, EventStoreNamespaceName canonical)
    {
        if (requested != canonical && _logged.TryAdd((eventStore, requested.Value), 0))
        {
            logger.Canonicalized(eventStore.Value, requested.Value, canonical.Value);
        }
    }
}
