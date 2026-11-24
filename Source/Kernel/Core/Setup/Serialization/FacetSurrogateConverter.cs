// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Converts between a <see cref="Facet"/> and its <see cref="FacetSurrogate"/>.
/// </summary>
[RegisterConverter]
public sealed class FacetSurrogateConverter : IConverter<Facet, FacetSurrogate>
{
    /// <inheritdoc/>
    public Facet ConvertFromSurrogate(in FacetSurrogate surrogate) => new(surrogate.Name, surrogate.Value);

    /// <inheritdoc/>
    public FacetSurrogate ConvertToSurrogate(in Facet value) => new(value.Name, value.Value);
}
