// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Carries a <see cref="Facet"/> between grains. The type is shared with the clients and does not reference Orleans.
/// </summary>
/// <param name="Name">The <see cref="Name"/> member of the <see cref="Facet"/>.</param>
/// <param name="Value">The <see cref="Value"/> member of the <see cref="Facet"/>.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Setup.Serialization.FacetSurrogate")]
public readonly record struct FacetSurrogate(
    [property: Id(0)] FacetName Name,
    [property: Id(1)] FacetValue Value);
