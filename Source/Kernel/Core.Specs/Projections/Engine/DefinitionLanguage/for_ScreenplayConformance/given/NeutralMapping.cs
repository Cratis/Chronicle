// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// A mapping onto one read-model property.
/// </summary>
/// <param name="Target">The dotted target path.</param>
/// <param name="Operation">The operation.</param>
/// <param name="Source">The value read, or null for operations without one.</param>
public record NeutralMapping(string Target, NeutralOperation Operation, NeutralValue? Source);
