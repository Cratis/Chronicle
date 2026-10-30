// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Initialization completed.
/// </summary>
/// <param name="Receipts">The durable evidence.</param>
[GenerateSerializer]
[Alias(nameof(GenerationInitializationEnabled))]
public sealed record GenerationInitializationEnabled([property: Id(0)] IReadOnlyList<GenerationInitializationReceipt> Receipts) : GenerationInitializationResult;
