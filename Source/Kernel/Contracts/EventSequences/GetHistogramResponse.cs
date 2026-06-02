// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for getting an event sequence histogram.
/// </summary>
[ProtoContract]
public class GetHistogramResponse
{
    /// <summary>
    /// Gets or sets the ordered collection of histogram buckets.
    /// </summary>
    [ProtoMember(1)]
    public IList<HistogramBucket> Buckets { get; set; } = [];
}
