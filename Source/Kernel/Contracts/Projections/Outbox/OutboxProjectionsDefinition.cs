// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Contracts.Events;
using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Projections.Outbox;

/// <summary>
/// Represents the definition of outbox projections.
/// </summary>
[ProtoContract]
public class OutboxProjectionsDefinition
{
    /// <summary>
    /// Gets or sets the projections per target event type.
    /// </summary>
    [ProtoMember(1)]
    public IDictionary<EventType, ProjectionDefinition> TargetEventTypeProjections { get; set; }
}
