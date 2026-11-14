// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents a stored generation whose schema the registration would change.
/// </summary>
/// <param name="Generation">The generation whose schema differs.</param>
/// <param name="Difference">The first difference that is not tolerated.</param>
sealed record SchemaProblem(uint Generation, SchemaDifference Difference);
