// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the command for retrying every failed partition of an observer.
/// </summary>
[ProtoContract]
public class RetryFailedPartitions : IObserverCommand
{
    /// <summary>
    /// Gets or sets the event store the observer belongs to.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace the observer belongs to.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the observer.
    /// </summary>
    [ProtoMember(3)]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence the observer observes; defaults to the event log when empty.
    /// </summary>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;
}
