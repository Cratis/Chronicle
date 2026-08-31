// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Placement;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternMiner"/> backed by a <see cref="LossyCountingSketch"/> per
/// scope, owned by one activation per event store and namespace.
/// </summary>
/// <remarks>
/// <para>
/// One sketch per grouping key rather than one for everybody: a habit is a person's, and a shared sketch would let
/// a busy user's routine outvote everyone else's while making "what does this user usually do" unanswerable.
/// Whether a global sketch should exist alongside these, for org-wide behavior, is deliberately left open.
/// </para>
/// <para>
/// Mining is in memory and cheap; persisting a scope rewrites everything that currently survives for it. Doing
/// that per mined batch couples the write cost to the event rate times the size of each scope's behavior, so
/// persistence is deferred: mining marks the scope dirty, and a timer rewrites what the interval touched on the
/// <see cref="PatternDetection.PersistenceInterval"/> cadence. A failed flush keeps the scopes dirty and answers
/// nothing but the log - the next tick simply tries again, and rewriting a scope is idempotent.
/// </para>
/// <para>
/// The activation dies with its silo while what survived it is persisted, so a scope acting for the first time in
/// an activation's life has its established patterns restored into the sketch before anything is mined - a fresh
/// sketch would hold its first events with full support, and the next flush would rewrite the scope from that,
/// wiping established behavior. A small tail of events can still be re-mined after a crash, because observer
/// progress is checkpointed in batches; that over-counts by at most the checkpoint window, and occurrences are a
/// bounded approximation.
/// </para>
/// <para>
/// The grain prefers local placement so it activates on the silo of the subscriber feeding it, keeping the call
/// per batch in-process rather than a network hop.
/// </para>
/// </remarks>
/// <param name="vocabulary">The <see cref="IFacetVocabulary"/> deciding which facets take part.</param>
/// <param name="generator">The <see cref="IFacetSetGenerator"/> expanding facets into candidate itemsets.</param>
/// <param name="storage">The <see cref="IStorage"/> to restore from and persist surviving patterns to.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[PreferLocalPlacement]
public class PatternMiner(
    IFacetVocabulary vocabulary,
    IFacetSetGenerator generator,
    IStorage storage,
    IOptions<ChronicleOptions> options,
    ILogger<PatternMiner> logger) : Grain, IPatternMiner
{
    readonly Dictionary<PatternGroupingKey, LossyCountingSketch> _sketches = [];
    readonly HashSet<PatternGroupingKey> _touchedScopes = [];
    readonly HashSet<PatternGroupingKey> _restoredScopes = [];
    readonly PatternDetection _configuration = options.Value.PatternDetection;
    PatternMinerKey _key = PatternMinerKey.NotSet;

    IBehaviorPatternStorage Patterns => storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Patterns;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = PatternMinerKey.Parse(this.GetPrimaryKeyString());
        var interval = TimeSpan.FromSeconds(Math.Max(1, _configuration.PersistenceInterval));
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
    public async Task Mine(IEnumerable<EventFeatures> features)
    {
        var mined = features.Where(feature => feature.GroupingKey.IsSpecified).ToArray();

        // Every scope the batch touches is restored before anything is mined, so a restore failure part-way
        // through fails the whole batch with nothing counted - the redelivery counts nothing twice.
        await RestoreScopesActingForTheFirstTime(mined.Select(feature => feature.GroupingKey));

        foreach (var feature in mined)
        {
            var facets = vocabulary.Select(feature);
            if (facets.IsEmpty)
            {
                continue;
            }

            var itemsets = generator.Generate(facets, _configuration.MaximumCombinationSize);
            GetSketch(feature.GroupingKey).Observe(itemsets, feature.Occurred);
            _touchedScopes.Add(feature.GroupingKey);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BehaviorPattern>> GetSurvivingPatterns(PatternGroupingKey groupingKey) =>
        Task.FromResult<IEnumerable<BehaviorPattern>>(
            _sketches.TryGetValue(groupingKey, out var sketch)
                ? [.. Surviving(groupingKey, sketch)]
                : []);

    /// <inheritdoc/>
    public async Task Persist()
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
                var surviving = (await GetSurvivingPatterns(scope)).ToArray();
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

    /// <summary>
    /// Add the context entries a restore has to synthesize, each with the frequency its patterns say it must
    /// have had.
    /// </summary>
    /// <param name="established">The persisted <see cref="BehaviorPattern">patterns</see> being restored.</param>
    /// <param name="entries">The entries being restored, keyed by itemset - contexts already among them are left alone.</param>
    /// <remarks>
    /// Only surviving patterns were persisted, but re-deriving a pattern's confidence needs the frequency of its
    /// context - the itemset without its action - and a pure context rarely survives on its own, because its
    /// confidence is just its support. Every number needed is still recoverable from what was written: confidence
    /// was frequency over context frequency, so every missing context entry is synthesized back from the patterns
    /// that reference it. Several patterns can share one context, and each names the same context frequency
    /// through its own confidence; the largest recovered answer is kept as the most precise against rounding.
    /// </remarks>
    static void SynthesizeMissingContexts(BehaviorPattern[] established, Dictionary<FacetSetKey, LossyCountingEntry> entries)
    {
        var contexts = new Dictionary<FacetSetKey, (FacetSet Context, long Frequency, BehaviorPattern Pattern)>();

        foreach (var pattern in established.Where(pattern => pattern.Facets.ConstrainsAction && pattern.Confidence.Value > 0d))
        {
            var context = pattern.Facets.WithoutActions();
            if (context.IsEmpty || entries.ContainsKey(context.Key))
            {
                continue;
            }

            var frequency = (long)Math.Round(pattern.Occurrences.Value / pattern.Confidence.Value);
            if (!contexts.TryGetValue(context.Key, out var existing) || existing.Frequency < frequency)
            {
                contexts[context.Key] = (context, frequency, pattern);
            }
        }

        foreach (var (context, frequency, pattern) in contexts.Values)
        {
            entries[context.Key] = new LossyCountingEntry(
                context,
                frequency,
                0L,
                pattern.Weight,
                pattern.FirstSeen,
                pattern.LastSeen);
        }
    }

    async Task RestoreScopesActingForTheFirstTime(IEnumerable<PatternGroupingKey> scopes)
    {
        foreach (var scope in scopes.Distinct().Where(scope => !_restoredScopes.Contains(scope)))
        {
            var established = (await Patterns.GetForScope(scope)).ToArray();
            Restore(scope, established);
            _restoredScopes.Add(scope);
        }
    }

    /// <summary>
    /// Seed a scope with the patterns an earlier activation had established, unless the scope already holds live
    /// counts - live counts are more current than what was persisted from them, so a restore into them is ignored
    /// rather than merged.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> to restore.</param>
    /// <param name="established">The persisted <see cref="BehaviorPattern">patterns</see> to restore from.</param>
    void Restore(PatternGroupingKey groupingKey, BehaviorPattern[] established)
    {
        if (_sketches.ContainsKey(groupingKey))
        {
            return;
        }

        var sketch = new LossyCountingSketch(_configuration.Error, _configuration.DecayFactor);

        if (established.Length > 0)
        {
            // Support was written as frequency over observations, so the observation count the sketch had is
            // recoverable from any pattern - the largest answer is the most precise against rounding.
            var observed = established.Max(pattern => pattern.Support.Value > 0d
                ? (long)Math.Round(pattern.Occurrences.Value / pattern.Support.Value)
                : 0L);

            var entries = established.ToDictionary(
                pattern => pattern.Facets.Key,
                pattern => new LossyCountingEntry(
                    pattern.Facets,
                    pattern.Occurrences,
                    0L,
                    pattern.Weight,
                    pattern.FirstSeen,
                    pattern.LastSeen));

            SynthesizeMissingContexts(established, entries);
            sketch.Restore(entries.Values, observed);
        }

        _sketches[groupingKey] = sketch;
    }

    LossyCountingSketch GetSketch(PatternGroupingKey groupingKey)
    {
        if (!_sketches.TryGetValue(groupingKey, out var sketch))
        {
            sketch = new LossyCountingSketch(_configuration.Error, _configuration.DecayFactor);
            _sketches[groupingKey] = sketch;
        }

        return sketch;
    }

    /// <summary>
    /// Gets the patterns of a scope that clear both thresholds.
    /// </summary>
    /// <param name="groupingKey">The <see cref="PatternGroupingKey"/> the sketch belongs to.</param>
    /// <param name="sketch">The <see cref="LossyCountingSketch"/> to read.</param>
    /// <returns>The surviving <see cref="BehaviorPattern">patterns</see>.</returns>
    /// <remarks>
    /// Confidence is read as the rule "in this context, this action follows": the frequency of the whole itemset
    /// over the frequency of the same itemset without its action facet. An itemset that names no action is pure
    /// context, and the only honest answer to "how often does this hold" is its support.
    /// </remarks>
    IEnumerable<BehaviorPattern> Surviving(PatternGroupingKey groupingKey, LossyCountingSketch sketch)
    {
        if (sketch.Observed == 0)
        {
            yield break;
        }

        foreach (var entry in sketch.Entries)
        {
            var support = (double)entry.Frequency / sketch.Observed;
            if (support < _configuration.MinimumSupport)
            {
                continue;
            }

            var confidence = ConfidenceOf(entry, sketch, support);
            if (confidence < _configuration.MinimumConfidence)
            {
                continue;
            }

            yield return new BehaviorPattern(
                groupingKey,
                entry.Itemset,
                entry.Frequency,
                confidence,
                support,
                entry.Weight,
                entry.FirstSeen,
                entry.LastSeen);
        }
    }

    double ConfidenceOf(LossyCountingEntry entry, LossyCountingSketch sketch, double support)
    {
        if (!entry.Itemset.ConstrainsAction)
        {
            return support;
        }

        var context = entry.Itemset.WithoutActions();
        if (context.IsEmpty)
        {
            return support;
        }

        var contextFrequency = sketch.GetFrequency(context.Key);
        return contextFrequency == 0 ? 0d : (double)entry.Frequency / contextFrequency;
    }
}
