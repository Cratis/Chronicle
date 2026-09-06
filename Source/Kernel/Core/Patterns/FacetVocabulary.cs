// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Represents an implementation of <see cref="IFacetVocabulary"/> driven by <see cref="PatternDetection"/>.
/// </summary>
/// <param name="options">The <see cref="IOptions{TOptions}"/> holding the <see cref="ChronicleOptions"/>.</param>
[Singleton]
public class FacetVocabulary(IOptions<ChronicleOptions> options) : IFacetVocabulary
{
    /// <inheritdoc/>
    public IReadOnlyList<FacetName> Facets { get; } =
        [.. options.Value.PatternDetection.Facets.Select(facet => new FacetName(facet))];

    /// <inheritdoc/>
    public FacetSet Select(EventFeatures features)
    {
        var facets = features.AsFacets();
        return new FacetSet(Facets
            .Where(facets.ContainsKey)
            .Select(name => new Facet(name, facets[name])));
    }

    /// <inheritdoc/>
    public FacetSet Select(FacetSet context) =>
        new(context.Facets.Where(facet => Facets.Contains(facet.Name)));
}
