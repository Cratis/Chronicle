// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Reports whether initialization is disabled, complete, or durably blocked.
/// </summary>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationResult))]
public abstract record GenerationInitializationResult;

