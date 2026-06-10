// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents a constraint violation from a reactor side-effect.
/// </summary>
/// <param name="EventTypeId">The event type ID that violated the constraint.</param>
/// <param name="Message">The constraint violation message.</param>
public record ReactorConstraintViolation(string EventTypeId, string Message);
