// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type and property constraint.
/// </summary>
/// <param name="EventType">The <see cref="EventType"/>.</param>
/// <param name="Schema">The <see cref="JsonSchema"/> for the event type.</param>
/// <param name="Property">The property on the event type.</param>
public record UniqueConstraintEventDefinition(EventType EventType, JsonSchema Schema, string Property);
