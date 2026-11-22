// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition of a child projection based on one property in an event.
/// </summary>
/// <param name="Event"><see cref="EventType"/> the property is on.</param>
/// <param name="PropertyExpression"><see cref="PropertyExpression"/> within the event.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Definitions.FromEventPropertyDefinition")]
public record FromEventPropertyDefinition([Id(0)] EventType Event, [Id(1)] PropertyExpression PropertyExpression);
