// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCaches"/>.
/// </summary>
[Singleton]
public class EventSequenceCaches : IEventSequenceCaches
{
    readonly ConcurrentDictionary<(MicroserviceId, TenantId, EventSequenceId), IEventSequenceCache> _caches = new();

    /// <inheritdoc/>
    public IEventSequenceCache GetFor(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId)
    {
        var key = (microserviceId, tenantId, eventSequenceId);
        if (!_caches.TryGetValue(key, out var cache))
        {
            cache = new EventSequenceCache();
            _caches.TryAdd(key, cache);
        }

        return cache;
    }
}
