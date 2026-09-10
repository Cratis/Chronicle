// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for getting the number of events per time bucket in an event sequence.
/// </summary>
[ProtoContract]
public class GetHistogramResponse
{
    /// <summary>
    /// Gets or sets the buckets that contain at least one event, ordered by time ascending.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<HistogramBucket> Buckets { get; set; } = [];
}
