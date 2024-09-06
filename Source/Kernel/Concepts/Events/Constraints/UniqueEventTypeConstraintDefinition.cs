// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="EventTypeId">The <see cref="EventTypeId"/> and properties the constraint is for.</param>
public record UniqueEventTypeConstraintDefinition(
    ConstraintName Name,
    EventTypeId EventTypeId) : IConstraintDefinition;
