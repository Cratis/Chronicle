// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Identifies one attempt of a kernel constraint rebuild.
/// </summary>
/// <param name="Value">The job step identifier.</param>
/// <param name="Attempt">The per-attempt nonce, or empty for an outside-worker cancellation.</param>
/// <param name="Started">When the attempt started, from the step silo's clock; used to order late deliveries.</param>
/// <param name="JobId">The owning job, or null from earlier kernels, whose liveness cannot be determined.</param>
[GenerateSerializer]
public sealed record ConstraintIndexRebuildRunId(
    [property: Id(0)] Guid Value,
    [property: Id(1)] Guid Attempt = default,
    [property: Id(2)] DateTimeOffset Started = default,
    [property: Id(3)] JobId? JobId = null);
