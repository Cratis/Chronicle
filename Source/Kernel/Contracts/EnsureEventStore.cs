// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Represents the request for ensuring an event store exists.
/// </summary>
[ProtoContract]
public record EnsureEventStore
{
    /// <summary>
    /// The event store to ensure namespaces for.
    /// </summary>
    [ProtoMember(1)]
    public string Name { get; set; }
}
