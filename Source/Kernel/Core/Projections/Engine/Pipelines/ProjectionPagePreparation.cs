// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Plans only known IO-free keys and parent reads, leaving routing and creation fallback to ordinary handling.
/// </summary>
/// <param name="storage">The replay-scoped event sequence.</param>
/// <param name="sink">The pipeline-owned sink.</param>
internal sealed class ProjectionPagePreparation(IEventSequenceStorage storage, ISink sink)
{
    /// <summary>Warms optional read caches without memoizing routing decisions.</summary>
    /// <param name="projection">The projection being replayed.</param>
    /// <param name="events">The complete page in delivery order.</param>
    /// <returns>A task representing preparation.</returns>
    internal async Task Prepare(IProjection projection, IReadOnlyList<AppendedEvent> events)
    {
        var reads = new PageParentReads(storage, events[0].Context.SequenceNumber, events.Max(@event => @event.Context.SequenceNumber.Value));
        var roots = new List<Key>();
        do
        {
            roots.Clear();
            foreach (var @event in events)
            {
                if (!projection.HasKeyResolverFor(@event.Context.EventType)) continue;
                var resolver = projection.GetKeyResolverFor(@event.Context.EventType);
                var key = await PageKeyResolver.Plan(resolver, @event, reads);
                if (key?.Value is not null) roots.Add(key with { ArrayIndexers = ArrayIndexers.NoIndexers });
            }
            if (!reads.HasPending) break;
            await reads.Load();
        }
        while (true);

        if (sink is ISinkPagePrefetch prefetch)
        {
            await prefetch.PrefetchPage(roots);
        }
    }
}
