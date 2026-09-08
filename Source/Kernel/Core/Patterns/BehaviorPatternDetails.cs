// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents a recurring combination of facets mined from an event store's history.
/// </summary>
/// <param name="Id">The identity of the pattern within its scope - the canonical facet set key.</param>
/// <param name="GroupingKey">The scope the pattern belongs to.</param>
/// <param name="Facets">The facets the pattern constrains, keyed by facet name.</param>
/// <param name="Confidence">How often the pattern holds when its context is present, in the range 0 to 1.</param>
/// <param name="Support">The share of all observed events the pattern was seen in, in the range 0 to 1.</param>
/// <param name="Occurrences">How many times the pattern has been observed.</param>
/// <param name="Weight">The recency-weighted strength of the pattern.</param>
/// <param name="Specificity">How many facets the pattern constrains.</param>
/// <param name="FirstSeen">When the pattern was first observed.</param>
/// <param name="LastSeen">When the pattern was last observed.</param>
/// <remarks>
/// Named for what it carries rather than for the concept, because <see cref="BehaviorPattern"/> in
/// <c>Concepts.Patterns</c> already owns that name.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.Patterns)]
public record BehaviorPatternDetails(
    string Id,
    string GroupingKey,
    IDictionary<string, string> Facets,
    double Confidence,
    double Support,
    long Occurrences,
    double Weight,
    int Specificity,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen)
{
    /// <summary>
    /// The number of matches returned when the caller does not ask for a specific number.
    /// </summary>
    /// <remarks>
    /// Small on purpose. The question is "what usually happens here", and an answer long enough to scroll is not
    /// one - the ranking already puts the most specific, most confident patterns first.
    /// </remarks>
    const int DefaultMaximumResults = 10;

    /// <summary>
    /// Gets the patterns a scope has established that match a context.
    /// </summary>
    /// <param name="eventStore">The event store to get patterns for.</param>
    /// <param name="namespace">The namespace to get patterns for.</param>
    /// <param name="groupingKey">The scope to get patterns within.</param>
    /// <param name="context">The partial context to match against, keyed by facet name.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns.</param>
    /// <param name="vocabulary">The <see cref="IFacetVocabulary"/> discarding facets that are not mined.</param>
    /// <param name="generator">The <see cref="IFacetSetGenerator"/> expanding a context into candidate itemsets.</param>
    /// <param name="matcher">The <see cref="IPatternMatcher"/> ranking the matches.</param>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
    /// <param name="minimumConfidence">The lowest confidence a returned pattern may hold.</param>
    /// <param name="maximumResults">The largest number of patterns to return.</param>
    /// <returns>The matching patterns.</returns>
    internal static async Task<IEnumerable<BehaviorPatternDetails>> MatchingPatterns(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        PatternGroupingKey groupingKey,
        IDictionary<string, string> context,
        IStorage storage,
        IFacetVocabulary vocabulary,
        IFacetSetGenerator generator,
        IPatternMatcher matcher,
        IOptions<ChronicleOptions> options,
        double minimumConfidence = 0d,
        int maximumResults = 0)
    {
        if (!groupingKey.IsSpecified)
        {
            return [];
        }

        var configuration = options.Value.PatternDetection;
        var requested = vocabulary.Select(context.ToFacetSet());

        // The stored patterns are combinations of facets, so a context is answered by looking up the same
        // combinations it can form - which is a bounded, keyed read rather than a scan of the scope.
        var candidates = generator
            .Generate(requested, configuration.MaximumCombinationSize)
            .Select(itemset => itemset.Key);

        var patterns = await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace)
            .Patterns
            .GetMatching(groupingKey, candidates);

        return matcher
            .Match(patterns, requested, ResolveConfidence(minimumConfidence, configuration), ResolveMaximumResults(maximumResults))
            .ToDetails();
    }

    /// <summary>
    /// Gets what a scope usually does in a context.
    /// </summary>
    /// <param name="eventStore">The event store to get patterns for.</param>
    /// <param name="namespace">The namespace to get patterns for.</param>
    /// <param name="groupingKey">The scope to get patterns within.</param>
    /// <param name="context">The partial context to match against, keyed by facet name.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns.</param>
    /// <param name="vocabulary">The <see cref="IFacetVocabulary"/> discarding facets that are not mined.</param>
    /// <param name="matcher">The <see cref="IPatternMatcher"/> ranking the matches.</param>
    /// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
    /// <param name="minimumConfidence">The lowest confidence a returned pattern may hold.</param>
    /// <param name="maximumResults">The largest number of patterns to return.</param>
    /// <returns>The patterns naming what usually happens.</returns>
    /// <remarks>
    /// Reads the scope rather than looking up candidate keys. An answer is the asked context plus an action, and
    /// the action's value is the thing being asked for - so its key cannot be formed in advance the way
    /// <see cref="MatchingPatterns"/> forms its candidates. What makes the read affordable is the mining bound
    /// rather than the query: an itemset only reaches storage by holding MinimumSupport of everything the scope
    /// did, so a scope holds patterns in the hundreds no matter how many events it produced.
    /// </remarks>
    internal static async Task<IEnumerable<BehaviorPatternDetails>> UsualActions(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        PatternGroupingKey groupingKey,
        IDictionary<string, string> context,
        IStorage storage,
        IFacetVocabulary vocabulary,
        IPatternMatcher matcher,
        IOptions<ChronicleOptions> options,
        double minimumConfidence = 0d,
        int maximumResults = 0)
    {
        if (!groupingKey.IsSpecified)
        {
            return [];
        }

        var configuration = options.Value.PatternDetection;
        var requested = vocabulary.Select(context.ToFacetSet());

        var patterns = await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace)
            .Patterns
            .GetForScope(groupingKey);

        return matcher
            .MatchActions(patterns, requested, ResolveConfidence(minimumConfidence, configuration), ResolveMaximumResults(maximumResults))
            .ToDetails();
    }

    /// <summary>
    /// Gets every pattern a scope has established.
    /// </summary>
    /// <param name="eventStore">The event store to get patterns for.</param>
    /// <param name="namespace">The namespace to get patterns for.</param>
    /// <param name="groupingKey">The scope to get patterns for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns.</param>
    /// <returns>The patterns the scope has established.</returns>
    internal static async Task<IEnumerable<BehaviorPatternDetails>> PatternsForScope(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        PatternGroupingKey groupingKey,
        IStorage storage) =>
        (await storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace)
            .Patterns
            .GetForScope(groupingKey))
        .ToDetails();

    /// <summary>
    /// Gets every pattern established in a namespace, across every scope.
    /// </summary>
    /// <param name="eventStore">The event store to get patterns for.</param>
    /// <param name="namespace">The namespace to get patterns for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the mined patterns.</param>
    /// <returns>Every pattern in the namespace.</returns>
    /// <remarks>
    /// For a browsing surface that treats the scope as one facet among the others rather than as something chosen
    /// before anything can be shown. Every pattern carries its <see cref="GroupingKey"/>, so which scope a pattern
    /// belongs to becomes just another thing to filter or pivot by.
    /// <para>
    /// The scopes are read first and then asked for concurrently, because the engine indexes patterns by scope and
    /// has no single read across all of them. That is a fair trade for an operator browsing a namespace, and a
    /// reason to add a kernel-side read if a deployment ever has enough scopes for the fan-out to hurt.
    /// </para>
    /// </remarks>
    internal static async Task<IEnumerable<BehaviorPatternDetails>> AllPatterns(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IStorage storage)
    {
        var patterns = storage.GetEventStore(eventStore).GetNamespace(@namespace).Patterns;
        var scopes = await patterns.GetScopes();
        var perScope = await Task.WhenAll(scopes.Select(patterns.GetForScope));

        return perScope.SelectMany(_ => _).ToDetails();
    }

    static PatternConfidence ResolveConfidence(double requested, PatternDetection configuration) =>
        requested > 0d ? new PatternConfidence(requested) : new PatternConfidence(configuration.MinimumConfidence);

    static int ResolveMaximumResults(int requested) => requested > 0 ? requested : DefaultMaximumResults;
}
