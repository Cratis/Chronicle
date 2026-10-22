// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>The identical requirement was already committed; nothing changed.</summary>
/// <param name="Floor">The existing floor.</param>
public sealed record GenerationFloorAlreadyAtOrAbove(GenerationFloor Floor);
