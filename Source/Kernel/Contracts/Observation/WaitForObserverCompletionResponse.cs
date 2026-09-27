// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the result of waiting for observers to complete for an append operation.
/// </summary>
[ProtoContract]
public class WaitForObserverCompletionResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether all observers completed successfully.
    /// </summary>
    [ProtoMember(1)]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the failed partitions discovered while waiting.
    /// </summary>
    [ProtoMember(2)]
    public IEnumerable<FailedPartition> FailedPartitions { get; set; } = new List<FailedPartition>();

    /// <summary>
    /// Gets or sets a value indicating that the wait expired before every observer completed or failed.
    /// </summary>
    [ProtoMember(3)]
    public bool TimedOut { get; set; }

    /// <summary>
    /// Gets or sets the identifiers of observers still outstanding when the wait expired.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<string> OutstandingObservers { get; set; } = [];
}
