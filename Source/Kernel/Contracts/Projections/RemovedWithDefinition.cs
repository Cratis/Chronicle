// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;
using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Projections;

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
    public EventType Event { get; set; }
}
