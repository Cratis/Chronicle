// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternCaptureSubscriber"/> that mines every event it observes and
/// persists the patterns that survive.
/// </summary>
/// <param name="miner">The <see cref="IPatternMiner"/> to mine with.</param>
/// <param name="extractor">The <see cref="IEventFeatureExtractor"/> for reading an event's contextual facts.</param>
/// <param name="storage">The <see cref="IStorage"/> to persist surviving patterns to.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <remarks>
/// <para>
/// Nothing per event is written. A batch is mined in memory and then the scopes it touched are rewritten from what
/// currently survives, so storage holds the scope's behavior as it now stands rather than a log of how it got
/// there.
/// </para>
/// <para>
/// Only the scopes a batch touched are rewritten. Rewriting every scope on every batch would make the write cost
/// grow with how many people have ever used the store rather than with how many acted just now.
/// </para>
/// </remarks>
public class PatternCaptureSubscriber(
    IPatternMiner miner,
    IEventFeatureExtractor extractor,
    IStorage storage,
    ILogger<PatternCaptureSubscriber> logger) : Grain, IPatternCaptureSubscriber
{
    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var batch = events.ToArray();
        if (batch.Length == 0)
        {
            return ObserverSubscriberResult.Ok(EventSequenceNumber.Unavailable);
        }

        var key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());

        try
        {
            var touched = new HashSet<Concepts.Patterns.PatternGroupingKey>();
            foreach (var @event in batch)
            {
                var features = extractor.Extract(@event);
                if (!features.GroupingKey.IsSpecified)
                {
                    continue;
                }

                miner.Observe(features);
                touched.Add(features.GroupingKey);
            }

            if (touched.Count > 0)
            {
                await Persist(key.EventStore, key.Namespace, touched);
            }

            return ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber);
        }
        catch (Exception ex)
        {
            // Mining is derived, secondary information. A failure here must not stop the event sequence being
            // observed for everything else, so it is reported as this observer's failure and nothing more.
            logger.FailedCapturingPatterns(key.EventStore, key.Namespace, ex);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    async Task Persist(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEnumerable<Concepts.Patterns.PatternGroupingKey> scopes)
    {
        var patterns = storage.GetEventStore(eventStore).GetNamespace(@namespace).Patterns;

        foreach (var scope in scopes)
        {
            var surviving = miner.GetSurvivingPatterns(scope).ToArray();
            await patterns.Save(surviving);
            await patterns.RemoveAllExcept(scope, surviving.Select(pattern => pattern.Facets.Key));
        }
    }
}
