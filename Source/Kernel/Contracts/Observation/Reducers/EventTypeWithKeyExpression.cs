// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Events;

using ProtoBuf;

namespace Cratis.Kernel.Contracts.Observation.Reducers;

/// <summary>
/// Represents the definition of an event type with key.
/// </summary>
[ProtoContract]
public class EventTypeWithKeyExpression
{
    /// <summary>
    /// Gets or sets the <see cref="EventType"/>.
    /// </summary>
    [ProtoMember(1)]
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the key expression.
    /// </summary>
    [ProtoMember(2)]
    public string Key { get; set; }
}
