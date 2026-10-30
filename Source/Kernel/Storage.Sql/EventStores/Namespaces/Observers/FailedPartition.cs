// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

/// <summary>
/// Represents a failed partition for SQL storage.
/// </summary>
/// <param name="Id">Unique identifier of the failed partition.</param>
/// <param name="Partition">The partition that is failed.</param>
/// <param name="ObserverId">The observer identifier for which this is a failed partition.</param>
/// <param name="Attempts">Collection of failed partition attempts.</param>
/// <param name="IsResolved">Whether the failure is resolved.</param>
public record FailedPartition(
    string Id,
    string Partition,
    string ObserverId,
    IEnumerable<FailedPartitionAttempt> Attempts,
    bool IsResolved)
{
    /// <summary>
    /// Gets whether the partition is quarantined.
    /// </summary>
    public bool IsQuarantined { get; init; }

    /// <summary>Gets the explicit webhook receiver policy.</summary>
    public WebhookFailureDisposition? WebhookDisposition { get; init; }

    /// <summary>Gets the last automatic recovery that reset the episode budget.</summary>
    public Guid? WebhookRecoveryId { get; init; }

    /// <summary>
    /// Gets the number of attempts made before the retry budget was last reset.
    /// </summary>
    public int AttemptsBeforeBudgetReset { get; init; }

    /// <summary>
    /// Gets the initialized recovery cursor, absent on legacy rows.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FailedPartitionRecoveryCursor? RecoveryCursor { get; init; }

    /// <summary>
    /// Gets the initialized recovery owner, absent on legacy rows.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FailedPartitionRecoveryOwner? RecoveryOwner { get; init; }

    /// <summary>
    /// Gets the optional handler hold.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FailedPartitionHold? Hold { get; init; }
}
