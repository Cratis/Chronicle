// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IFacetSetGenerator"/>.
/// </summary>
/// <remarks>
/// Combinations are capped rather than exhaustive on purpose. Every subset of a facet set is a candidate the sketch
/// has to count, and the number of subsets doubles with each facet; capping the size at k leaves a polynomial
/// number of candidates - the sum of the binomial coefficients up to k - which is what keeps mining affordable per
/// event.
/// </remarks>
[Singleton]
public class FacetSetGenerator : IFacetSetGenerator
{
    /// <inheritdoc/>
    public IEnumerable<FacetSet> Generate(FacetSet source, int maximumCombinationSize)
    {
        if (source.IsEmpty || maximumCombinationSize <= 0)
        {
            return [];
        }

        var facets = source.Facets;
        var largest = Math.Min(maximumCombinationSize, facets.Count);
        var combinations = new List<FacetSet>();

        for (var length = 1; length <= largest; length++)
        {
            Build(facets, new Facet[length], 0, 0, combinations);
        }

        return combinations;
    }

    static void Build(IReadOnlyList<Facet> facets, Facet[] buffer, int index, int start, List<FacetSet> combinations)
    {
        if (index == buffer.Length)
        {
            combinations.Add(new FacetSet(buffer));
            return;
        }

        for (var current = start; current <= facets.Count - (buffer.Length - index); current++)
        {
            buffer[index] = facets[current];
            Build(facets, buffer, index + 1, current + 1, combinations);
        }
    }
}
