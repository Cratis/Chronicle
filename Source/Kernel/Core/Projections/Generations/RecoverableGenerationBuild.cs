// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Represents a cataloged Building generation's recoverable intent or explicit recovery refusal.
/// </summary>
/// <param name="Record">The original catalog record.</param>
/// <param name="Request">The original persisted request, present only when recovery is allowed.</param>
/// <param name="Refusal">The refusal reason, absent when the original request is recoverable.</param>
public sealed record RecoverableGenerationBuild(MaterializationGenerationRecord Record, GenerationBuildRequest? Request = null, GenerationBuildRecoveryRefusal? Refusal = null);
