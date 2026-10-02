// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from clearing the quarantine of a failed partition.
/// </summary>
[ProtoContract]
public class ClearPartitionQuarantineResponse
{
    /// <summary>
    /// Gets or sets the <see cref="ClearPartitionQuarantineOutcome"/> describing what happened.
    /// </summary>
    [ProtoMember(1)]
    public ClearPartitionQuarantineOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="PartitionRecoveryOutcome"/> of starting the retry. Only meaningful when
    /// <see cref="ClearPartitionQuarantine.RetryImmediately"/> was set and the quarantine was cleared.
    /// </summary>
    [ProtoMember(2)]
    public PartitionRecoveryOutcome RetryOutcome { get; set; }
}
