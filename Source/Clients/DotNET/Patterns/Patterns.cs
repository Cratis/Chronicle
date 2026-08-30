// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Contracts;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IPatterns"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/> the patterns belong to.</param>
public class Patterns(IEventStore eventStore) : IPatterns
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task<IEnumerable<BehaviorPattern>> GetPatterns(
        PatternGroupingKey groupingKey,
        FacetSet context,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default)
    {
        var patterns = await _servicesAccessor.Services.Patterns.GetPatterns(
            new()
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                GroupingKey = groupingKey,
                Context = context.Facets.ToDictionary(facet => facet.Name.Value, facet => facet.Value.Value),

                // Zero means "whatever the server is configured for" on both of these. The client does not carry
                // its own copy of the thresholds - a default duplicated here would silently disagree with the
                // server's the moment either changed.
                MinimumConfidence = minimumConfidence?.Value ?? 0d,
                MaximumResults = maximumResults
            },
            new CallContext(new CallOptions(cancellationToken: cancellationToken)));

        return patterns.ToClient();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BehaviorPattern>> GetUsualActions(
        PatternGroupingKey groupingKey,
        FacetSet context,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default)
    {
        var patterns = await _servicesAccessor.Services.Patterns.GetUsualActions(
            new()
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                GroupingKey = groupingKey,
                Context = context.Facets.ToDictionary(facet => facet.Name.Value, facet => facet.Value.Value),
                MinimumConfidence = minimumConfidence?.Value ?? 0d,
                MaximumResults = maximumResults
            },
            new CallContext(new CallOptions(cancellationToken: cancellationToken)));

        return patterns.ToClient();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<BehaviorPattern>> GetPatternsAt(
        PatternGroupingKey groupingKey,
        DateTimeOffset? moment = default,
        FacetSet? alsoConstraining = default,
        PatternConfidence? minimumConfidence = default,
        int maximumResults = 0,
        CancellationToken cancellationToken = default)
    {
        var at = moment ?? DateTimeOffset.Now;

        // Built on top of whatever the caller already wanted to constrain, so asking about a moment and asking
        // about the kind of work are the same question rather than two competing ones. With() replaces, so a caller
        // who constrained the day themselves gets the moment's day - the value they asked to be asked about.
        var context = (alsoConstraining ?? FacetSet.Empty)
            .With(FacetName.Day, at.DayOfWeek.ToString())
            .With(FacetName.TimeBucket, at.ToTimeBucket().ToString());

        return GetUsualActions(groupingKey, context, minimumConfidence, maximumResults, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BehaviorPattern>> GetPatternsForScope(
        PatternGroupingKey groupingKey,
        CancellationToken cancellationToken = default)
    {
        var patterns = await _servicesAccessor.Services.Patterns.GetPatternsForScope(
            new()
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace,
                GroupingKey = groupingKey
            },
            new CallContext(new CallOptions(cancellationToken: cancellationToken)));

        return patterns.ToClient();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PatternGroupingKey>> GetScopes(CancellationToken cancellationToken = default)
    {
        var scopes = await _servicesAccessor.Services.Patterns.GetScopes(
            new()
            {
                EventStore = eventStore.Name,
                Namespace = eventStore.Namespace
            },
            new CallContext(new CallOptions(cancellationToken: cancellationToken)));

        return [.. scopes.Select(scope => new PatternGroupingKey(scope))];
    }
}
