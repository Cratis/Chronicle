// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.Definitions;

/// <summary>
/// Represents the definition of a child projection based on one property in an event.
/// </summary>
public class FromEventPropertyDefinition
{
    /// <summary>
    /// Gets or sets the <see cref="EventType"/> the property is on.
    /// </summary>
    public EventType Event { get; set; } = EventType.Unknown;

    /// <summary>
    /// Gets or sets the <see cref="PropertyExpression"/> within the event.
    /// </summary>
    public PropertyExpression PropertyExpression { get; set; } = string.Empty;
}
