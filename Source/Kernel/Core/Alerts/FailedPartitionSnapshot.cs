// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Represents a failed partition of an observer as the alert evaluator sees it.
/// </summary>
/// <remarks>
/// Only partitions that are still failing are part of a snapshot. A resolved one is absent, and
/// <see cref="ObserverAlertSnapshot.Endings"/> says why it left.
/// </remarks>
/// <param name="Id">The <see cref="FailedPartitionId"/>, which is also the identifier of the incident raised for it.</param>
/// <param name="Partition">The <see cref="AlertPartition"/> that is failing.</param>
/// <param name="FirstAttempt">When the partition first failed.</param>
/// <param name="LastAttempt">When the partition last failed.</param>
/// <param name="AttemptCount">How many times the partition has failed.</param>
/// <param name="AttemptsInCurrentBudget">How many of the attempts count against the current retry budget, which is
/// the attempts since the quarantine of the partition was last cleared.</param>
/// <param name="IsQuarantined">Whether the partition has run out of retries and will not be retried automatically.</param>
/// <param name="FailureKind">What kind of thing went wrong on the latest failure.</param>
/// <param name="Message">The first message of the latest failure.</param>
public record FailedPartitionSnapshot(
    FailedPartitionId Id,
    AlertPartition Partition,
    DateTimeOffset FirstAttempt,
    DateTimeOffset LastAttempt,
    int AttemptCount,
    int AttemptsInCurrentBudget,
    bool IsQuarantined,
    FailureKind FailureKind,
    string Message);
