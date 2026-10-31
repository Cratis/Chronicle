// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One read-model level: the projection's read model, a child element type or a nested object type.
/// </summary>
/// <param name="Schema">The schema of the level.</param>
/// <param name="Child">Resolves the level a child collection or nested object property projects into.</param>
public record ConformanceLevel(JsonSchema Schema, Func<string, ConformanceLevel?> Child);
