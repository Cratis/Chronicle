// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the delegate for providing a message for a constraint violation.
/// </summary>
/// <param name="violation"><see cref="ConstraintViolation"/> to provide for.</param>
/// <returns>Provided <see cref="ConstraintViolationMessage"/>.</returns>
public delegate ConstraintViolationMessage ConstraintViolationMessageProvider(ConstraintViolation violation);
