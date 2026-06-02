// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the payload for completing a stream within an event sequence.
/// </summary>
[ProtoContract]
public class CompleteStreamRequest : IEventSequenceRequest
{
    /// <inheritdoc/>
    [ProtoMember(1, IsRequired = true)]
    public string EventStore { get; set; } = string.Empty;

    /// <inheritdoc/>
    [ProtoMember(2, IsRequired = true)]
    public string Namespace { get; set; } = string.Empty;

    /// <inheritdoc/>
    [ProtoMember(3, IsRequired = true)]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream type that identifies the stream to complete.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public string EventStreamType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event stream identifier within the stream type to complete.
    /// </summary>
    [ProtoMember(5, IsRequired = true)]
    public string EventStreamId { get; set; } = string.Empty;
}
