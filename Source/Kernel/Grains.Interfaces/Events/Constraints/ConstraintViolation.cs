// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents a violation of a constraint.
/// </summary>
/// <param name="EventTypeId">The <see cref="EventTypeId"/> that caused the violation.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> where the violation occurred.</param>
/// <param name="ConstraintType"><see cref="ConstraintType"/> that was violated.</param>
/// <param name="ConstraintName"><see cref="ConstraintName"/> that was violated.</param>
/// <param name="Message"><see cref="ConstraintViolationMessage"/> with the message.</param>
/// <param name="Details"><see cref="ConstraintViolationDetails"/> with more details associated with the violation.</param>
public record ConstraintViolation(
    EventTypeId EventTypeId,
    EventSequenceNumber SequenceNumber,
    ConstraintType ConstraintType,
    ConstraintName ConstraintName,
    ConstraintViolationMessage Message,
    ConstraintViolationDetails Details);
