// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the request for clearing the quarantine of a single failed partition of an observer.
/// </summary>
[ProtoContract]
public class ClearPartitionQuarantine : IObserverCommand
{
    /// <inheritdoc/>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3)]
    public string ObserverId { get; set; }

    /// <inheritdoc/>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quarantined partition to clear.
    /// </summary>
    [ProtoMember(5)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to start recovering the partition immediately after clearing.
    /// </summary>
    [ProtoMember(6)]
    public bool RetryImmediately { get; set; }
}
