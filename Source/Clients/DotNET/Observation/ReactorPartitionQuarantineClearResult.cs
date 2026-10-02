// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Describes the result of clearing the quarantine of a failed observer partition.
/// </summary>
/// <param name="Outcome">Whether the quarantine was cleared.</param>
/// <param name="RetryOutcome">The outcome of starting the retry. Only meaningful when a retry was requested and the quarantine was cleared.</param>
public record ReactorPartitionQuarantineClearResult(
    ReactorPartitionQuarantineClearOutcome Outcome,
    ReactorPartitionRetryOutcome RetryOutcome);
