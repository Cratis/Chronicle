// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternCaptureSubscriber"/> that mines every event it observes and
/// persists the patterns that survive.
/// </summary>
/// <param name="miner">The <see cref="IPatternMiner"/> to mine with.</param>
/// <param name="extractor">The <see cref="IEventFeatureExtractor"/> for reading an event's contextual facts.</param>
/// <param name="storage">The <see cref="IStorage"/> to persist surviving patterns to.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <remarks>
/// <para>
/// Observing only mines - in memory, cheap - and marks the scope dirty. A timer persists what the interval
/// touched. Persisting a scope means rewriting everything that currently survives for it, so doing it per
/// observed batch couples the write cost to the event rate times the size of each scope's behavior: a bulk
/// ingest of a few thousand events becomes hundreds of thousands of storage writes, all serialized through the
/// one activation an unpartitioned subscriber has, and every delivery queued behind them times out. Deferring to
/// the timer bounds the write cost by how many scopes acted in the interval, no matter how many events they
/// produced.
/// </para>
/// <para>
/// The first time a scope acts in this activation's life, its established patterns are restored into the miner
/// before anything is mined. The sketch dies with its process while the patterns it survived into do not, so a
/// scope mined from zero would rewrite its established behavior with whatever happened right after a restart.
/// When the restore cannot be read, the batch is failed and redelivered rather than mined - nothing has been
/// counted yet, so the retry counts nothing twice. A small tail of events can still be re-mined after a crash,
/// because observer progress is checkpointed in batches; that over-counts by at most the checkpoint window, and
/// occurrences are a bounded approximation - a bounded overlap is the cost of continuing instead of starting
/// over.
/// </para>
/// <para>
/// A failed persist keeps the scopes dirty and answers nothing but the log - the next tick simply tries again.
/// Failing the observation instead would fail partitions and redeliver events that were already mined, doubling
/// their counts, over state that is derived and rewritten in full on the next flush anyway.
/// </para>
/// <para>
/// Only the scopes an interval touched are rewritten. Rewriting every scope on every flush would make the write
/// cost grow with how many people have ever used the store rather than with how many acted just now.
/// </para>
/// </remarks>
public class PatternCaptureSubscriber(
    IPatternMiner miner,
    IEventFeatureExtractor extractor,
    IStorage storage,
    IOptions<ChronicleOptions> options,
    ILogger<PatternCaptureSubscriber> logger) : Grain, IPatternCaptureSubscriber
{
    readonly HashSet<PatternGroupingKey> _touchedScopes = [];
    readonly HashSet<PatternGroupingKey> _restoredScopes = [];
    ObserverSubscriberKey _key = ObserverSubscriberKey.Unspecified;

    IBehaviorPatternStorage Patterns => storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Patterns;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PatternDetection.PersistenceInterval));
        this.RegisterGrainTimer(Persist, new GrainTimerCreationOptions { DueTime = interval, Period = interval });
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await Persist();
        await base.OnDeactivateAsync(reason, cancellationToken);
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
            var mined = batch
                .Select(extractor.Extract)
                .Where(features => features.GroupingKey.IsSpecified)
                .ToArray();

            await RestoreScopesActingForTheFirstTime(mined.Select(features => features.GroupingKey));

            foreach (var features in mined)
            {
                miner.Observe(_key.EventStore, _key.Namespace, features);
                _touchedScopes.Add(features.GroupingKey);
            }

            return ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber);
        }
        catch (Exception ex)
        {
            // Mining is derived, secondary information. A failure here must not stop the event sequence being
            // observed for everything else, so it is reported as this observer's failure and nothing more.
            logger.FailedCapturingPatterns(_key.EventStore, _key.Namespace, ex);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }

    async Task RestoreScopesActingForTheFirstTime(IEnumerable<PatternGroupingKey> scopes)
    {
        foreach (var scope in scopes.Distinct().Where(scope => !_restoredScopes.Contains(scope)))
        {
            var established = await Patterns.GetForScope(scope);
            miner.Restore(_key.EventStore, _key.Namespace, scope, established);
            _restoredScopes.Add(scope);
        }
    }

    async Task Persist()
    {
        if (_touchedScopes.Count == 0)
        {
            return;
        }

        var scopes = _touchedScopes.ToArray();
        _touchedScopes.Clear();

        try
        {
            var patterns = Patterns;

            foreach (var scope in scopes)
            {
                var surviving = miner.GetSurvivingPatterns(_key.EventStore, _key.Namespace, scope).ToArray();
                await patterns.Save(surviving);
                await patterns.RemoveAllExcept(scope, surviving.Select(pattern => pattern.Facets.Key));
            }
        }
        catch (Exception ex)
        {
            // Rewriting a scope is idempotent, so scopes that did get written before the failure are simply
            // rewritten again on the next tick along with the ones that did not.
            _touchedScopes.UnionWith(scopes);
            logger.FailedPersistingPatterns(_key.EventStore, _key.Namespace, scopes.Length, ex);
        }
    }
}
