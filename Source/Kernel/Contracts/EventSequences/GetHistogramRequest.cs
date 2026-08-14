// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for getting the number of events per time bucket in an event sequence.
/// </summary>
[ProtoContract]
public class GetHistogramRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; }

    /// <inheritdoc/>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; }

    /// <inheritdoc/>
    [ProtoMember(3, IsRequired = true)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the criteria narrowing the events counted.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public EventSequenceQueryCriteria Criteria { get; set; } = new();

    /// <summary>
    /// Gets or sets the time bucket size.
    /// </summary>
    [ProtoMember(5)]
    public HistogramResolution Resolution { get; set; }
}
