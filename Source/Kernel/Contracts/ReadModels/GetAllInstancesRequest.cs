// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for getting all instances of a read model by processing events.
/// </summary>
[ProtoContract]
public class GetAllInstancesRequest
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace of the read model.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the read model.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModelIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the event sequence ID.
    /// </summary>
    [ProtoMember(4), DefaultValue("event-log")]
    public string EventSequenceId { get; set; } = "event-log";

    /// <summary>
    /// Gets or sets the maximum number of events to process. If not set or set to ulong.MaxValue, all events will be processed.
    /// </summary>
    [ProtoMember(5), DefaultValue(ulong.MaxValue)]
    public ulong EventCount { get; set; } = ulong.MaxValue;
}
