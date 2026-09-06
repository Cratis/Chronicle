// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.MongoDB.Patterns;

/// <summary>
/// Represents the MongoDB representation of a <see cref="Facet"/>.
/// </summary>
public class PatternFacet
{
    /// <summary>
    /// Gets or sets the <see cref="FacetName"/>.
    /// </summary>
    public FacetName Name { get; set; } = FacetName.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="FacetValue"/>.
    /// </summary>
    public FacetValue Value { get; set; } = FacetValue.Unspecified;
}
