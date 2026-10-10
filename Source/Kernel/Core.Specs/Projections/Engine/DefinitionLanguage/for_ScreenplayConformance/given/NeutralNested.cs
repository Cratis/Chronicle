// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// A nested object.
/// </summary>
/// <param name="Property">The object property.</param>
/// <param name="Scope">The level of the nested object.</param>
public record NeutralNested(string Property, NeutralScope Scope);
