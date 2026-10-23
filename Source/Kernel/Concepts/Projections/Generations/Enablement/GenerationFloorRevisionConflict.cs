// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The expected revision did not match.</summary>
/// <param name="Current">The current floor, or null when absent.</param>
public sealed record GenerationFloorRevisionConflict(GenerationFloor? Current);
