// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of what removes an element in a child relationship.
/// </summary>
[ProtoContract]
public class RemovedWithDefinition
{
    /// <summary>
    /// Gets or sets the event that is causing the removal.
    /// </summary>
    [ProtoMember(1)]
    public EventType Event { get; set; }
}
