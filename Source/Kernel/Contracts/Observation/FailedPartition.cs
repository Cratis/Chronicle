// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents a failed partition.
/// </summary>
[ProtoContract]
public class FailedPartition
{
    /// <summary>
    /// Gets or sets the unique identifier of the failed partition.
    /// </summary>
    [ProtoMember(1)]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the observer id.
    /// </summary>
    [ProtoMember(2)]
    public string ObserverId { get; set; }

    /// <summary>
    /// Gets or sets the partition that is failed.
    /// </summary>
    [ProtoMember(3)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets the attempts for the failed partition.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<FailedPartitionAttempt> Attempts { get; set; }
}
