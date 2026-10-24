// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Concepts.Projections.Generations;

/// <summary>
/// Identifies a retryable reservation of immutable generation execution inputs.
/// </summary>
/// <param name="Key">The materialization.</param>
/// <param name="Execution">The immutable execution inputs.</param>
/// <param name="Operation">The caller-persisted operation identity.</param>
/// <param name="At">The time the operation was first requested.</param>
public sealed record GenerationBuildRequest(MaterializationKey Key, MaterializationExecutionSpecification Execution, GenerationBuildOperationId Operation, DateTimeOffset At);
