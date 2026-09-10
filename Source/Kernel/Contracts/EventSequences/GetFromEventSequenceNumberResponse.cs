// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for getting events from an event sequence number and optionally for an event source id and specific event types.
/// </summary>
[ProtoContract]
public class GetFromEventSequenceNumberResponse
{
    /// <summary>
    /// Gets or sets the events.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public IList<AppendedEvent> Events { get; set; } = [];
}
