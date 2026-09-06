// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that expands a set of facets into the candidate itemsets mining counts.
/// </summary>
public interface IFacetSetGenerator
{
    /// <summary>
    /// Generate every non-empty combination of facets up to a given size.
    /// </summary>
    /// <param name="source">The <see cref="FacetSet"/> to combine facets from.</param>
    /// <param name="maximumCombinationSize">The largest number of facets a combination may hold.</param>
    /// <returns>The candidate <see cref="FacetSet">itemsets</see>, ordered from least to most specific.</returns>
    IEnumerable<FacetSet> Generate(FacetSet source, int maximumCombinationSize);
}
