// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternMiner"/> backed by a <see cref="LossyCountingSketch"/> per
/// scope within an event store's namespace.
/// </summary>
/// <remarks>
/// <para>
/// One sketch per grouping key rather than one for everybody: a habit is a person's, and a shared sketch would let
/// a busy user's routine outvote everyone else's while making "what does this user usually do" unanswerable.
/// Whether a global sketch should exist alongside these, for org-wide behavior, is deliberately left open.
/// </para>
/// <para>
/// Sketches are keyed by event store and namespace as well as by scope, because the miner is one instance serving
/// every store the server holds. The same scope name in two stores - or two tenants' namespaces - is two different
/// people's behavior; a sketch keyed by scope alone would count them together and contaminate both stores'
/// persisted patterns.
/// </para>
/// <para>
/// An event nobody can be named for is not mined at all. Its behavior belongs to no scope, so it could only be
/// counted into a catch-all that every unattributed append in the store would pour into - which is noise, not a
/// pattern.
/// </para>
/// </remarks>
/// <param name="vocabulary">The <see cref="IFacetVocabulary"/> deciding which facets take part.</param>
/// <param name="generator">The <see cref="IFacetSetGenerator"/> expanding facets into candidate itemsets.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
[Singleton]
public class PatternMiner(
    IFacetVocabulary vocabulary,
    IFacetSetGenerator generator,
    IOptions<ChronicleOptions> options) : IPatternMiner
{
    readonly Dictionary<(EventStoreName EventStore, EventStoreNamespaceName Namespace, PatternGroupingKey GroupingKey), LossyCountingSketch> _sketches = [];
    readonly Lock _lock = new();
    readonly PatternDetection _configuration = options.Value.PatternDetection;

    /// <inheritdoc/>
    public void Observe(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventFeatures features)
    {
        if (!features.GroupingKey.IsSpecified)
        {
            return;
        }

        var facets = vocabulary.Select(features);
        if (facets.IsEmpty)
        {
            return;
        }

        var itemsets = generator.Generate(facets, _configuration.MaximumCombinationSize);

        lock (_lock)
        {
            GetSketch(eventStore, @namespace, features.GroupingKey).Observe(itemsets, features.Occurred);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Live counts win: restoring is only meaningful into the absence a restart leaves behind. Once a scope holds a
    /// sketch, whatever it has counted is more current than what was persisted from it, so a restore into it is
    /// ignored rather than merged.
    /// </para>
    /// <para>
    /// Only surviving patterns were persisted, but re-deriving a pattern's confidence needs the frequency of its
    /// context - the itemset without its action - and a pure context rarely survives on its own, because its
    /// confidence is just its support. Every number needed is still recoverable from what was written: support was
    /// frequency over observations and confidence was frequency over context frequency, so the observation count
    /// and every missing context entry are synthesized back from the patterns that reference them. Without that,
    /// every restored pattern whose context was absent would re-derive at zero confidence and be swept away on the
    /// first flush after a restart - precisely the wipe restoring exists to prevent.
    /// </para>
    /// </remarks>
    public void Restore(EventStoreName eventStore, EventStoreNamespaceName @namespace, PatternGroupingKey groupingKey, IEnumerable<BehaviorPattern> patterns)
    {
        lock (_lock)
        {
            var key = (eventStore, @namespace, groupingKey);
            if (_sketches.ContainsKey(key))
            {
                return;
            }

            var sketch = new LossyCountingSketch(_configuration.Error, _configuration.DecayFactor);
            var established = patterns.ToArray();

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

            _sketches[key] = sketch;
        }
    }

    /// <inheritdoc/>
    public void Decay(DateTimeOffset asOf)
    {
        lock (_lock)
        {
            foreach (var sketch in _sketches.Values)
            {
                sketch.Decay(asOf);
                sketch.Prune();
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<BehaviorPattern> GetSurvivingPatterns(EventStoreName eventStore, EventStoreNamespaceName @namespace, PatternGroupingKey groupingKey)
    {
        lock (_lock)
        {
            return _sketches.TryGetValue((eventStore, @namespace, groupingKey), out var sketch)
                ? [.. Surviving(groupingKey, sketch)]
                : [];
        }
    }

    /// <summary>
    /// Add the context entries a restore has to synthesize, each with the frequency its patterns say it must
    /// have had.
    /// </summary>
    /// <param name="established">The persisted <see cref="BehaviorPattern">patterns</see> being restored.</param>
    /// <param name="entries">The entries being restored, keyed by itemset - contexts already among them are left alone.</param>
    /// <remarks>
    /// Several patterns can share one context, and each names the same context frequency through its own confidence;
    /// the largest recovered answer is kept as the most precise against rounding.
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

    LossyCountingSketch GetSketch(EventStoreName eventStore, EventStoreNamespaceName @namespace, PatternGroupingKey groupingKey)
    {
        var key = (eventStore, @namespace, groupingKey);
        if (!_sketches.TryGetValue(key, out var sketch))
        {
            sketch = new LossyCountingSketch(_configuration.Error, _configuration.DecayFactor);
            _sketches[key] = sketch;
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
