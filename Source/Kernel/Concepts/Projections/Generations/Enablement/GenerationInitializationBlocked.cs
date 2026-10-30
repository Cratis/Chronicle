// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Initialization cannot proceed.
/// </summary>
/// <param name="Blockers">The durable evidence.</param>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationBlocked))]
public sealed record GenerationInitializationBlocked([property: Id(0)] IReadOnlyList<GenerationInitializationBlocker> Blockers) : GenerationInitializationResult;
