// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents a violation of a constraint.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> where the violation occurred.</param>
/// <param name="ConstraintType"><see cref="ConstraintType"/> that was violated.</param>
/// <param name="ConstraintName"><see cref="ConstraintName"/> that was violated.</param>
/// <param name="Message"><see cref="ViolationMessage"/> with more details.</param>
public record ConstraintViolation(EventSequenceNumber SequenceNumber, ConstraintType ConstraintType, ConstraintName ConstraintName, ViolationMessage Message);
