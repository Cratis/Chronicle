// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a violation of a constraint.
/// </summary>
/// <param name="EventType">The <see cref="EventType"/> where the violation occurred.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> where the violation occurred.</param>
/// <param name="ConstraintName"><see cref="ConstraintName"/> that was violated.</param>
/// <param name="Message"><see cref="ConstraintViolationMessage"/> with more details.</param>
/// <param name="Details"><see cref="ConstraintViolationDetails"/> with more details.</param>
public record ConstraintViolation(
    EventType EventType,
    EventSequenceNumber SequenceNumber,
    ConstraintName ConstraintName,
    ConstraintViolationMessage Message,
    ConstraintViolationDetails Details);
