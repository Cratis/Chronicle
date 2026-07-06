// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request to get a histogram of an event sequence.
/// </summary>
[ProtoContract]
public class GetHistogramRequest : IEventSequenceRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace name.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(3, IsRequired = true)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the histogram resolution.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public HistogramResolution Resolution { get; set; }

    /// <summary>
    /// Gets or sets the optional inclusive lower-bound timestamp filter.
    /// </summary>
    [ProtoMember(5)]
    public SerializableDateTimeOffset? From { get; set; }

    /// <summary>
    /// Gets or sets the optional inclusive upper-bound timestamp filter.
    /// </summary>
    [ProtoMember(6)]
    public SerializableDateTimeOffset? To { get; set; }

    /// <summary>
    /// Gets or sets the optional event types to filter for.
    /// </summary>
    [ProtoMember(7)]
    public IList<Events.EventType> EventTypes { get; set; } = [];
}
