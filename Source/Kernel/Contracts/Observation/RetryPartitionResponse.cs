// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from asking an observer to recover a specific failed partition.
/// </summary>
[ProtoContract]
public class RetryPartitionResponse
{
    /// <summary>
    /// Gets or sets the <see cref="PartitionRecoveryOutcome"/> describing what happened.
    /// </summary>
    [ProtoMember(1)]
    public PartitionRecoveryOutcome Outcome { get; set; }
}
