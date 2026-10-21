// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Identifies one attempt of a kernel constraint rebuild.
/// </summary>
/// <param name="Value">The stable job step identifier.</param>
/// <param name="Attempt">The attempt nonce, or empty for an outside-worker cancellation.</param>
/// <param name="Started">When the attempt began; a later attempt of the same step has a later value, which orders late deliveries.</param>
[GenerateSerializer]
public sealed record ConstraintIndexRebuildRunId(
    [property: Id(0)] Guid Value,
    [property: Id(1)] Guid Attempt = default,
    [property: Id(2)] DateTimeOffset Started = default);
