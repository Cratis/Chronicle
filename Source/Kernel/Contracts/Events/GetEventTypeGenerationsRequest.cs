// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Represents a request for getting all generations for a specific event type.
/// </summary>
[ProtoContract]
public class GetEventTypeGenerationsRequest
{
    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    [ProtoMember(2)]
    public string EventTypeId { get; set; } = string.Empty;
}
