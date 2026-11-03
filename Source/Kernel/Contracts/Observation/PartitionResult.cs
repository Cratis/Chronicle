// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the outcome of observing the events of one partition within a page.
/// </summary>
[ProtoContract]
public class PartitionResult
{
    /// <summary>
    /// Gets or sets the partition the outcome is for.
    /// </summary>
    [ProtoMember(1)]
    public string Partition { get; set; }

    /// <summary>
    /// Gets or sets the state of the partition after observing it.
    /// </summary>
    [ProtoMember(2)]
    public ObservationState State { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the last event of the partition that was observed successfully.
    /// </summary>
    [ProtoMember(3)]
    public ulong LastSuccessfulObservation { get; set; }

    /// <summary>
    /// Gets or sets the exception messages when the partition failed.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public IList<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the exception stack trace when the partition failed.
    /// </summary>
    [ProtoMember(5)]
    public string ExceptionStackTrace { get; set; }

    /// <summary>
    /// Gets or sets how long to wait, in milliseconds, before the partition is delivered again when it was deferred.
    /// </summary>
    [ProtoMember(6)]
    public ulong RetryAfterMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the reduced read model state as JSON, for reducers.
    /// </summary>
    [ProtoMember(7)]
    public string? ReadModelState { get; set; }
}
