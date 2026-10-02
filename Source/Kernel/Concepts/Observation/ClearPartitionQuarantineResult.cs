// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the result of clearing the quarantine of a failed partition.
/// </summary>
/// <param name="Outcome">The <see cref="ClearPartitionQuarantineOutcome"/> of clearing the quarantine.</param>
/// <param name="RetryOutcome">The <see cref="PartitionRecoveryOutcome"/> of starting the retry. Only meaningful when a retry was requested and <paramref name="Outcome"/> is <see cref="ClearPartitionQuarantineOutcome.Cleared"/>.</param>
[GenerateSerializer]
[Alias(nameof(ClearPartitionQuarantineResult))]
public record ClearPartitionQuarantineResult(
    ClearPartitionQuarantineOutcome Outcome,
    PartitionRecoveryOutcome RetryOutcome);
