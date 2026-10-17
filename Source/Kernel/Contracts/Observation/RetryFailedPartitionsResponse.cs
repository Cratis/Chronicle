// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from retrying the failed partitions of an observer.
/// </summary>
[ProtoContract]
public class RetryFailedPartitionsResponse
{
    /// <summary>
    /// Gets or sets the number of failed partitions the observer had when it was asked.
    /// </summary>
    [ProtoMember(1)]
    public int FailedPartitions { get; set; }
}
