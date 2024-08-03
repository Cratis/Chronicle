// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the message from a violation of a constraint.
/// </summary>
/// <param name="Message">The inner value.</param>
public record ConstraintViolationMessage(string Message) : ConceptAs<string>(Message)
{
    /// <summary>
    /// Represents a constraint violation message that is not defined.
    /// </summary>
    public static readonly ConstraintViolationMessage NotDefined = string.Empty;

    /// <summary>
    /// Implicitly convert from a <see cref="string"/> to a <see cref="ConstraintViolationMessage"/>.
    /// </summary>
    /// <param name="message">Name to convert.</param>
    public static implicit operator ConstraintViolationMessage(string message) => new(message);
}
