// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="FacetSet"/> and its <see cref="FacetSetSurrogate"/>.
/// </summary>
/// <remarks>
/// The set normalizes its facets and derives its key when it is created, so only the facets travel.
/// </remarks>
[RegisterConverter]
public sealed class FacetSetSurrogateConverter : IConverter<FacetSet, FacetSetSurrogate>
{
    /// <inheritdoc/>
    public FacetSet ConvertFromSurrogate(in FacetSetSurrogate surrogate) => new(surrogate.Facets);

    /// <inheritdoc/>
    public FacetSetSurrogate ConvertToSurrogate(in FacetSet value) => new([.. value.Facets]);
}
