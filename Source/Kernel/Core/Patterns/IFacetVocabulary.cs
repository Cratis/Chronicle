// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines the facets that take part in mining, and how to read them off an event.
/// </summary>
public interface IFacetVocabulary
{
    /// <summary>
    /// Gets the facets that take part in the mined itemset key.
    /// </summary>
    IReadOnlyList<FacetName> Facets { get; }

    /// <summary>
    /// Select the facets an event contributes to mining.
    /// </summary>
    /// <param name="features">The <see cref="EventFeatures"/> to select from.</param>
    /// <returns>A <see cref="FacetSet"/> holding the participating facets the event carries a value for.</returns>
    FacetSet Select(EventFeatures features);

    /// <summary>
    /// Select the facets of a context that take part in mining, discarding any the vocabulary does not mine.
    /// </summary>
    /// <param name="context">The <see cref="FacetSet"/> describing the context.</param>
    /// <returns>A <see cref="FacetSet"/> holding only the participating facets.</returns>
    FacetSet Select(FacetSet context);
}
