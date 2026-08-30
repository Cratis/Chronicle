// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using ContractIPatterns = Cratis.Chronicle.Contracts.Patterns.IPatterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns;

/// <summary>
/// Represents an implementation of <see cref="ContractIPatterns"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for getting the mined patterns.</param>
/// <param name="vocabulary"><see cref="IFacetVocabulary"/> for discarding facets that are not mined.</param>
/// <param name="generator"><see cref="IFacetSetGenerator"/> for expanding a context into candidate itemsets.</param>
/// <param name="matcher"><see cref="IPatternMatcher"/> for ranking the matches.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
internal sealed class Patterns(
    IStorage storage,
    IFacetVocabulary vocabulary,
    IFacetSetGenerator generator,
    IPatternMatcher matcher,
    IOptions<ChronicleOptions> options) : ContractIPatterns
{
    /// <summary>
    /// The number of matches returned when the caller does not ask for a specific number.
    /// </summary>
    /// <remarks>
    /// Small on purpose. The question is "what usually happens here", and an answer long enough to scroll is not
    /// one - the ranking already puts the most specific, most confident patterns first.
    /// </remarks>
    const int DefaultMaximumResults = 10;

    /// <inheritdoc/>
    public async Task<IEnumerable<Pattern>> GetPatterns(Contracts.Patterns.GetPatternsRequest request, CallContext context = default)
    {
        var groupingKey = new PatternGroupingKey(request.GroupingKey);
        if (!groupingKey.IsSpecified)
        {
            return [];
        }

        var configuration = options.Value.PatternDetection;
        var requested = vocabulary.Select(request.Context.ToFacetSet());

        // The stored patterns are combinations of facets, so a context is answered by looking up the same
        // combinations it can form - which is a bounded, keyed read rather than a scan of the scope.
        var candidates = generator
            .Generate(requested, configuration.MaximumCombinationSize)
            .Select(itemset => itemset.Key);

        var patterns = await storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .Patterns
            .GetMatching(groupingKey, candidates);

        var minimumConfidence = request.MinimumConfidence > 0d
            ? new PatternConfidence(request.MinimumConfidence)
            : new PatternConfidence(configuration.MinimumConfidence);

        var maximumResults = request.MaximumResults > 0 ? request.MaximumResults : DefaultMaximumResults;

        return matcher
            .Match(patterns, requested, minimumConfidence, maximumResults)
            .Select(pattern => pattern.ToContract())
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Pattern>> GetPatternsForScope(Contracts.Patterns.GetPatternsForScopeRequest request, CallContext context = default)
    {
        var patterns = await storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .Patterns
            .GetForScope(new PatternGroupingKey(request.GroupingKey));

        return patterns.Select(pattern => pattern.ToContract()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> GetScopes(Contracts.Patterns.GetPatternScopesRequest request, CallContext context = default)
    {
        var scopes = await storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .Patterns
            .GetScopes();

        return [.. scopes.Select(scope => scope.Value).Order(StringComparer.Ordinal)];
    }
}
