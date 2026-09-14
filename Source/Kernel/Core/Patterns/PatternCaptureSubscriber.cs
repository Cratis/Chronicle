// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternCaptureSubscriber"/> that extracts the contextual facts of
/// every event it observes and hands them to the <see cref="IPatternMiner"/> of its event store and namespace.
/// </summary>
/// <param name="extractor">The <see cref="IEventFeatureExtractor"/> for reading an event's contextual facts.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <remarks>
/// <para>
/// The subscriber is an adapter between the observer machinery and the miner: it extracts features and forwards
/// each delivered batch as one call to the miner grain resolved by its own key's event store and namespace. That
/// resolution is what keeps behavior isolated - the same scope name in two stores or two tenants' namespaces
/// reaches two different miner grains and can never count into one sketch. Sketches, restoration, and deferred
/// persistence all live on the miner.
/// </para>
/// <para>
/// A failed hand-off is reported as this observer's failure and nothing more - mining is derived, secondary
/// information, and a failure here must not stop the event sequence being observed for everything else. The miner
/// counts nothing when it fails, so the redelivered batch counts nothing twice.
/// </para>
/// </remarks>
public class PatternCaptureSubscriber(
    IEventFeatureExtractor extractor,
    ILogger<PatternCaptureSubscriber> logger) : Grain, IPatternCaptureSubscriber
{
    ObserverSubscriberKey _key = ObserverSubscriberKey.Unspecified;
    IPatternMiner _miner = null!;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _miner = GrainFactory.GetGrain<IPatternMiner>(new PatternMinerKey(_key.EventStore, _key.Namespace));
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var batch = events.ToArray();
        if (batch.Length == 0)
        {
            return ObserverSubscriberResult.Ok(EventSequenceNumber.Unavailable);
        }

        try
        {
            var features = batch
                .Select(extractor.Extract)
                .Where(feature => feature.GroupingKey.IsSpecified)
                .ToArray();

            if (features.Length > 0)
            {
                await _miner.Mine(features);
            }

            return ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber);
        }
        catch (Exception ex)
        {
            logger.FailedCapturingPatterns(_key.EventStore, _key.Namespace, ex);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }
}
