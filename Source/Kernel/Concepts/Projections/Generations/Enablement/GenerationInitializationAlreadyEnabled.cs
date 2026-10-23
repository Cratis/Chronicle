// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// A valid enabled receipt already exists.
/// </summary>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationAlreadyEnabled))]
public sealed record GenerationInitializationAlreadyEnabled : GenerationInitializationResult;
