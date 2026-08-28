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
}
