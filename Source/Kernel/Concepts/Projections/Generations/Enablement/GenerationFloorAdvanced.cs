// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The floor and audit were committed atomically.</summary>
/// <param name="Floor">The committed floor.</param>
public sealed record GenerationFloorAdvanced(GenerationFloor Floor);
