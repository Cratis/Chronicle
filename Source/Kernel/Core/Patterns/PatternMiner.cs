// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatternMiner"/> backed by a <see cref="LossyCountingSketch"/> per
/// scope.
/// </summary>
/// <remarks>
/// <para>
/// One sketch per grouping key rather than one for everybody: a habit is a person's, and a shared sketch would let
/// a busy user's routine outvote everyone else's while making "what does this user usually do" unanswerable.
/// Whether a global sketch should exist alongside these, for org-wide behavior, is deliberately left open.
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
    readonly Dictionary<PatternGroupingKey, LossyCountingSketch> _sketches = [];
    readonly Lock _lock = new();
    readonly PatternDetection _configuration = options.Value.PatternDetection;

    /// <inheritdoc/>
    public void Observe(EventFeatures features)
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
            GetSketch(features.GroupingKey).Observe(itemsets, features.Occurred);
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
    public IEnumerable<BehaviorPattern> GetSurvivingPatterns()
    {
        lock (_lock)
        {
            return [.. _sketches.SelectMany(pair => Surviving(pair.Key, pair.Value))];
        }
    }

    /// <inheritdoc/>
    public IEnumerable<BehaviorPattern> GetSurvivingPatterns(PatternGroupingKey groupingKey)
    {
        lock (_lock)
        {
            return _sketches.TryGetValue(groupingKey, out var sketch)
                ? [.. Surviving(groupingKey, sketch)]
                : [];
        }
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
        if (!entry.Itemset.Constrains(FacetName.CommandType))
        {
            return support;
        }

        var context = new FacetSet(entry.Itemset.Facets.Where(facet => facet.Name != FacetName.CommandType));
        if (context.IsEmpty)
        {
            return support;
        }

        var contextFrequency = sketch.GetFrequency(context.Key);
        return contextFrequency == 0 ? 0d : (double)entry.Frequency / contextFrequency;
    }
}
