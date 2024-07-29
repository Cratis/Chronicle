// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Represents the definition of a child projection based on one property in an event.
/// </summary>
[ProtoContract]
public class FromEventPropertyDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="EventType"/> the property is on.
    /// </summary>
    [ProtoMember(1)]
    public EventType Event { get; set; }

    /// <summary>
    /// Gets or sets the property expression within the event.
    /// </summary>
    [ProtoMember(2)]
    public string PropertyExpression { get; set; }
}
