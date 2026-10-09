// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Represents the request for dehydrating a read model session.
/// </summary>
[ProtoContract]
public class DehydrateSessionRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    [ProtoMember(3)]
    public string ReadModelIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence identifier.
    /// </summary>
    [ProtoMember(4)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model key.
    /// </summary>
    [ProtoMember(5)]
    public string ReadModelKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    [ProtoMember(6)]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional event source type filter.
    /// </summary>
    [ProtoMember(7)]
    public string EventSourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional event stream type filter.
    /// </summary>
    [ProtoMember(8)]
    public string EventStreamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional event stream identifier filter.
    /// </summary>
    [ProtoMember(9)]
    public string EventStreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event source identifier when different from the model key.
    /// </summary>
    [ProtoMember(10)]
    public string EventSourceId { get; set; } = string.Empty;
}
