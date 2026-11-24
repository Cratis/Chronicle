// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="FacetSet"/> between grains as its facets. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="Facets">The <see cref="Facet">facets</see> in the set.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.FacetSetSurrogate")]
public readonly record struct FacetSetSurrogate([property: Id(0)] IReadOnlyList<Facet> Facets);
