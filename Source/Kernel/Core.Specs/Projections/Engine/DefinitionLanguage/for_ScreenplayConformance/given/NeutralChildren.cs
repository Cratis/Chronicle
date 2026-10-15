// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// A child collection.
/// </summary>
/// <param name="Property">The collection property.</param>
/// <param name="IdentifiedBy">The element property identifying a child.</param>
/// <param name="Scope">The level of the children.</param>
public record NeutralChildren(string Property, string IdentifiedBy, NeutralScope Scope);
