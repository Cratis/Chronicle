// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the request for checking if there are events for a specific event source identifier.
/// </summary>
[ProtoContract]
public class HasEventsForEventSourceIdRequest : IEventSequenceRequest
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
    /// Gets or sets the event source identifier.
    /// </summary>
    [ProtoMember(4, IsRequired = true)]
    public string EventSourceId { get; set; }
}
