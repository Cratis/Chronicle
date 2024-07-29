// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Represents the response for checking if there are events for a specific event source identifier.
/// </summary>
[ProtoContract]
public class HasEventsForEventSourceIdResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether there are events or not.
    /// </summary>
    [ProtoMember(1)]
    public bool HasEvents { get; set; }
}
