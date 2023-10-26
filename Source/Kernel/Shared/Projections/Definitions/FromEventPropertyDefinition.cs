// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Projections.Definitions;

/// <summary>
/// Represents the definition of a child projection based on one property in an event.
/// </summary>
/// <param name="EventType"><see cref="EventType"/> the property is on.</param>
/// <param name="PropertyExpression"><see cref="PropertyExpression"/> within the event.</param>
public record FromEventPropertyDefinition(EventType EventType, PropertyExpression PropertyExpression);
