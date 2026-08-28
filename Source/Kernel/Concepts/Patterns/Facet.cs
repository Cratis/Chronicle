// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns;

/// <summary>
/// Represents a single contextual dimension and the value it held.
/// </summary>
/// <param name="Name">The <see cref="FacetName"/>.</param>
/// <param name="Value">The <see cref="FacetValue"/>.</param>
public record Facet(FacetName Name, FacetValue Value);
