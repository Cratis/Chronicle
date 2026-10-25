// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Describes candidate admission without starting its observer or changing publication.
/// </summary>
/// <param name="Result">The admission outcome.</param>
/// <param name="Record">The authoritative generation record, when available.</param>
/// <param name="Grant">The fixed writer grant, only when the operation is admitted.</param>
public sealed record GenerationBuildAdmission(GenerationBuildAdmissionResult Result, MaterializationGenerationRecord? Record = null, GenerationWriterGrant? Grant = null);
