// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a definition of a unique event type constraint.
/// </summary>
/// <param name="Name">Name of the constraint.</param>
/// <param name="MessageCallback">the callback that provides the <see cref="ConstraintViolationMessage"/> of the constraint.</param>
/// <param name="EventTypeId">The <see cref="EventTypeId"/> the constraint is for.</param>
/// <param name="RemovedWith">The <see cref="Events.EventTypeId"/> of the event that removes the constraint.</param>
public record UniqueEventTypeConstraintDefinition(
    ConstraintName Name,
    ConstraintViolationMessageProvider MessageCallback,
    EventTypeId EventTypeId,
    EventTypeId? RemovedWith) : IConstraintDefinition;
